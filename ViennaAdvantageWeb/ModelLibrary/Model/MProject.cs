﻿/********************************************************
 * Project Name   : VAdvantage
 * Class Name     : MProject
 * Purpose        : 
 * Class Used     : 
 * Chronological    Development
 * Raghunandan     17-Jun-2009
  ******************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VAdvantage.Classes;
using VAdvantage.Common;
using VAdvantage.Process;
using System.Windows.Forms;
using VAdvantage.Model; 
using VAdvantage.DataBase;
using VAdvantage.SqlExec;
using VAdvantage.Utility;
using System.Data;
using VAdvantage.Logging;

namespace VAdvantage.Model
{
    public class MProject : X_C_Project
    {
        /**	Cached PL			*/
        private int _M_PriceList_ID = 0;

        /**
     * 	Create new Project by copying
     * 	@param ctx context
     *	@param C_Project_ID project
     * 	@param dateDoc date of the document date
     *	@param trxName transaction
     *	@return Project
     */
        public static MProject CopyFrom(Ctx ctx, int C_Project_ID, DateTime? dateDoc, Trx trxName)
        {
            MProject from = new MProject(ctx, C_Project_ID, trxName);
            if (from.GetC_Project_ID() == 0)
                throw new ArgumentException("From Project not found C_Project_ID=" + C_Project_ID);
            //
            MProject to = new MProject(ctx, 0, trxName);
            PO.CopyValues(from, to, from.GetAD_Client_ID(), from.GetAD_Org_ID());
            to.Set_ValueNoCheck("C_Project_ID", I_ZERO);
            //	Set Value with Time
            String Value = to.GetValue() + " ";
            String Time = dateDoc.ToString();
            int length = Value.Length + Time.Length;
            if (length <= 40)
                Value += Time;
            else
                Value += Time.Substring(length - 40 - 1);
            to.SetValue(Value);
            to.SetInvoicedAmt(Env.ZERO);
            to.SetProjectBalanceAmt(Env.ZERO);
            to.SetProcessed(false);
            //
            if (!to.Save())
                throw new Exception("Could not create Project");

            if (to.CopyDetailsFrom(from) == 0)
                throw new Exception("Could not create Project Details");

            return to;
        }

       
        /*****
         * 	Standard Constructor
         *	@param ctx context
         *	@param C_Project_ID id
         *	@param trxName transaction
         */
        public MProject(Ctx ctx, int C_Project_ID, Trx trxName)
            : base(ctx, C_Project_ID, trxName)
        {

            if (C_Project_ID == 0)
            {
                //	setC_Project_ID(0);
                //	setValue (null);
                //	setC_Currency_ID (0);
                SetCommittedAmt(Env.ZERO);
                SetCommittedQty(Env.ZERO);
                SetInvoicedAmt(Env.ZERO);
                SetInvoicedQty(Env.ZERO);
                SetPlannedAmt(Env.ZERO);
                SetPlannedMarginAmt(Env.ZERO);
                SetPlannedQty(Env.ZERO);
                SetProjectBalanceAmt(Env.ZERO);
                //	setProjectCategory(PROJECTCATEGORY_General);
                SetProjInvoiceRule(PROJINVOICERULE_None);
                SetProjectLineLevel(PROJECTLINELEVEL_Project);
                SetIsCommitCeiling(false);
                SetIsCommitment(false);
                SetIsSummary(false);
                SetProcessed(false);
            }
        }

        /**
         * 	Load Constructor
         *	@param ctx context
         *	@param dr result set
         *	@param trxName transaction
         */
        public MProject(Ctx ctx, DataRow dr, Trx trxName)
            : base(ctx, dr, trxName)
        {

        }



        /**
         * 	Get Project Type as Int (is Button).
         *	@return C_ProjectType_ID id
         */
        public int GetC_ProjectType_ID_Int()
        {
            String pj = base.GetC_ProjectType_ID();
            if (pj == null)
                return 0;
            int C_ProjectType_ID = 0;
            try
            {
                C_ProjectType_ID = int.Parse(pj);
            }
            catch (Exception ex)
            {
               log.Log(Level.SEVERE, pj, ex);
            }
            return C_ProjectType_ID;
        }

        /**
         * 	Set Project Type (overwrite r/o)
         *	@param C_ProjectType_ID id
         */
        public void SetC_ProjectType_ID(int C_ProjectType_ID)
        {
            if (C_ProjectType_ID == 0)
                base.Set_Value("C_ProjectType_ID", null);
            else
                base.Set_Value("C_ProjectType_ID", (int)C_ProjectType_ID);
        }

        /**
         *	String Representation
         * 	@return info
         */
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder("MProject[").Append(Get_ID())
                .Append("-").Append(GetValue()).Append(",ProjectCategory=").Append(GetProjectCategory())
                .Append("]");
            return sb.ToString();
        }

        /**
         * 	Get Price List from Price List Version
         *	@return price list or 0
         */
        public new int GetM_PriceList_ID()
        {
            if (GetM_PriceList_Version_ID() == 0)
                return 0;
            if (_M_PriceList_ID > 0)
                return _M_PriceList_ID;
            //
            String sql = "SELECT M_PriceList_ID FROM M_PriceList_Version WHERE M_PriceList_Version_ID=" + GetM_PriceList_Version_ID();
            _M_PriceList_ID = DataBase.DB.GetSQLValue(null, sql);
            return _M_PriceList_ID;
        }

        /**
         * 	Set PL Version
         *	@param M_PriceList_Version_ID id
         */
        public new void SetM_PriceList_Version_ID(int M_PriceList_Version_ID)
        {
            base.SetM_PriceList_Version_ID(M_PriceList_Version_ID);
            _M_PriceList_ID = 0;	//	reset
        }


        /**************************************************************************
         * 	Get Project Lines
         *	@return Array of lines
         */
        public MProjectLine[] GetLines()
        {
            List<MProjectLine> list = new List<MProjectLine>();
            String sql = "SELECT * FROM C_ProjectLine WHERE C_Project_ID=" + GetC_Project_ID() + " ORDER BY Line";
            DataTable dt = null;
            IDataReader idr = null;
            try
            {
                idr = DataBase.DB.ExecuteReader(sql, null, Get_TrxName());
                dt = new DataTable();
                dt.Load(idr);
                idr.Close();
                foreach (DataRow dr in dt.Rows)
                {
                    list.Add(new MProjectLine(GetCtx(), dr, Get_TrxName()));
                }
            }
            catch (Exception ex)
            {
                if (idr != null)
                {
                    idr.Close();
                }
               log.Log(Level.SEVERE, sql, ex);
            }
            finally {
                if (idr != null)
                {
                    idr.Close();
                }
                dt = null;
            }

            MProjectLine[] retValue = new MProjectLine[list.Count];
            retValue = list.ToArray();
            return retValue;
        }

        /**
         * 	Get Project Issues
         *	@return Array of issues
         */
        public MProjectIssue[] GetIssues()
        {
            List<MProjectIssue> list = new List<MProjectIssue>();
            String sql = "SELECT * FROM C_ProjectIssue WHERE C_Project_ID=" + GetC_Project_ID() + " ORDER BY Line";
            DataTable dt = null;
            IDataReader idr = null;
            try
            {
                idr = DataBase.DB.ExecuteReader(sql, null, Get_TrxName());
                dt = new DataTable();
                dt.Load(idr);
                idr.Close();
                foreach (DataRow dr in dt.Rows)
                {
                    list.Add(new MProjectIssue(GetCtx(), dr, Get_TrxName()));
                }
            }
            catch (Exception ex)
            {
                if (idr != null)
                {
                    idr.Close();
                }
               log.Log(Level.SEVERE, sql, ex);
            }
            finally {
                if (idr != null)
                {
                    idr.Close();
                }
                dt = null;
            }
            MProjectIssue[] retValue = new MProjectIssue[list.Count];
            retValue = list.ToArray();
            return retValue;
        }

        /**
         * 	Get Project Phases
         *	@return Array of phases
         */
        public MProjectPhase[] GetPhases()
        {
            List<MProjectPhase> list = new List<MProjectPhase>();
            String sql = "SELECT * FROM C_ProjectPhase WHERE C_Project_ID=" + GetC_Project_ID() + " ORDER BY SeqNo";
            DataTable dt = null;
            IDataReader idr = null;
            try
            {
                idr = DataBase.DB.ExecuteReader(sql, null, Get_TrxName());
                dt = new DataTable();
                dt.Load(idr);
                idr.Close();
                foreach (DataRow dr in dt.Rows)
                {
                    list.Add(new MProjectPhase(GetCtx(), dr, Get_TrxName()));
                }
            }
            catch (Exception ex)
            {
                if (idr != null)
                {
                    idr.Close();
                }
               log.Log(Level.SEVERE, sql, ex);
            }
            finally
            {
                if (idr != null)
                {
                    idr.Close();
                }
                dt = null;
            }

            MProjectPhase[] retValue = new MProjectPhase[list.Count];
            retValue = list.ToArray();
            return retValue;
        }


        /**
         * 	Copy Lines/Phase/Task from other Project
         *	@param project project
         *	@return number of total lines copied
         */
        public int CopyDetailsFrom(MProject project)
        {
            if (IsProcessed() || project == null)
                return 0;
            int count = CopyLinesFrom(project)
                + CopyPhasesFrom(project);
            return count;
        }

        /**
         * 	Copy Lines From other Project
         *	@param project project
         *	@return number of lines copied
         */
        public int CopyLinesFrom(MProject project)
        {
            if (IsProcessed() || project == null)
                return 0;
            int count = 0;
            MProjectLine[] fromLines = project.GetLines();
            for (int i = 0; i < fromLines.Length; i++)
            {
                MProjectLine line = new MProjectLine(GetCtx(), 0, project.Get_TrxName());
                PO.CopyValues(fromLines[i], line, GetAD_Client_ID(), GetAD_Org_ID());
                line.SetC_Project_ID(GetC_Project_ID());
                line.SetInvoicedAmt(Env.ZERO);
                line.SetInvoicedQty(Env.ZERO);
                line.SetC_OrderPO_ID(0);
                line.SetC_Order_ID(0);
                line.SetProcessed(false);
                if (line.Save())
                    count++;
            }
            if (fromLines.Length != count)
            {
                log.Log(Level.SEVERE, "Lines difference - Project=" + fromLines.Length + " <> Saved=" + count);
            }

            return count;
        }

        /**
         * 	Copy Phases/Tasks from other Project
         *	@param fromProject project
         *	@return number of items copied
         */
        public int CopyPhasesFrom(MProject fromProject)
        {
            if (IsProcessed() || fromProject == null)
                return 0;
            int count = 0;
            int taskCount = 0;
            //	Get Phases
            MProjectPhase[] myPhases = GetPhases();
            MProjectPhase[] fromPhases = fromProject.GetPhases();
            //	Copy Phases
            for (int i = 0; i < fromPhases.Length; i++)
            {
                //	Check if Phase already exists
                int C_Phase_ID = fromPhases[i].GetC_Phase_ID();
                bool exists = false;
                if (C_Phase_ID == 0)
                    exists = false;
                else
                {
                    for (int ii = 0; ii < myPhases.Length; ii++)
                    {
                        if (myPhases[ii].GetC_Phase_ID() == C_Phase_ID)
                        {
                            exists = true;
                            break;
                        }
                    }
                }
                //	Phase exist
                if (exists)
                {
                    log.Info("Phase already exists here, ignored - " + fromPhases[i]);
                }
                else
                {
                    MProjectPhase toPhase = new MProjectPhase(GetCtx(), 0, Get_TrxName());
                    PO.CopyValues(fromPhases[i], toPhase, GetAD_Client_ID(), GetAD_Org_ID());
                    toPhase.SetC_Project_ID(GetC_Project_ID());
                    toPhase.SetC_Order_ID(0);
                    toPhase.SetIsComplete(false);
                    if (toPhase.Save())
                    {
                        count++;
                        taskCount += toPhase.CopyTasksFrom(fromPhases[i]);
                    }
                }
            }
            if (fromPhases.Length != count)
            {
                  log.Warning("Count difference - Project=" + fromPhases.Length + " <> Saved=" + count);
            }

            return count + taskCount;
        }

        /**
         *	Set Project Type and Category.
         * 	If Service Project copy Projet Type Phase/Tasks
         *	@param type project type
         */
        public void SetProjectType(MProjectType type)
        {
            if (type == null)
                return;
            SetC_ProjectType_ID(type.GetC_ProjectType_ID());
            SetProjectCategory(type.GetProjectCategory());
            if (PROJECTCATEGORY_ServiceChargeProject.Equals(GetProjectCategory()))
                CopyPhasesFrom(type);
        }

        /**
         *	Copy Phases from Type
         *	@param type Project Type
         *	@return count
         */
        public int CopyPhasesFrom(MProjectType type)
        {
            //	create phases
            int count = 0;
            int taskCount = 0;
            MProjectTypePhase[] typePhases = type.GetPhases();
            for (int i = 0; i < typePhases.Length; i++)
            {
                MProjectPhase toPhase = new MProjectPhase(this, typePhases[i]);
                if (toPhase.Save())
                {
                    count++;
                    taskCount += toPhase.CopyTasksFrom(typePhases[i]);
                }
            }
           log.Fine("#" + count + "/" + taskCount
               + " - " + type);
            if (typePhases.Length != count)
            {
               log.Log(Level.SEVERE, "Count difference - Type=" + typePhases.Length + " <> Saved=" + count);
            }
            return count;
        }

        /**
         * 	Before Save
         *	@param newRecord new
         *	@return true
         */
        protected override bool BeforeSave(bool newRecord)
        {
            if (GetAD_User_ID() == -1)	//	Summary Project in Dimensions
                SetAD_User_ID(0);

            //	Set Currency
            if (Is_ValueChanged("M_PriceList_Version_ID") && GetM_PriceList_Version_ID() != 0)
            {
                MPriceList pl = MPriceList.Get(GetCtx(), GetM_PriceList_ID(), null);
                if (pl != null && pl.Get_ID() != 0)
                    SetC_Currency_ID(pl.GetC_Currency_ID());
            }
            return true;
        }

        /**
         * 	After Save
         *	@param newRecord new
         *	@param success success
         *	@return success
         */
        protected override bool AfterSave(bool newRecord, bool success)
        {
            if (newRecord & success)
            {
                Insert_Accounting("C_Project_Acct", "C_AcctSchema_Default", null);
            }

            //	Value/Name change
            MProject prjph = null;
            if (success && !newRecord
                && (Is_ValueChanged("Value") || Is_ValueChanged("Name")))
                MAccount.UpdateValueDescription(GetCtx(), "C_Project_ID=" + GetC_Project_ID(), Get_TrxName());
            if (GetC_Campaign_ID() != 0)
            {
                MCampaign cam = new MCampaign(GetCtx(), GetC_Campaign_ID(), null);
                decimal plnAmt = Util.GetValueOfDecimal(DB.ExecuteScalar("SELECT COALESCE(SUM(pl.PlannedAmt),0)  FROM C_Project pl WHERE pl.IsActive = 'Y' AND pl.C_Campaign_ID = " + GetC_Campaign_ID()));
                cam.SetCosts(plnAmt);
                cam.Save();
            }
            else
            {
                prjph = new MProject(GetCtx(), GetC_Project_ID(), Get_Trx());
                decimal plnAmt = Util.GetValueOfDecimal(DB.ExecuteScalar("SELECT COALESCE(SUM(PlannedAmt),0) FROM C_ProjectPhase WHERE IsActive= 'Y' AND C_Project_ID= " + GetC_Project_ID()));
                DB.ExecuteQuery("UPDATE C_Project SET PlannedAmt=" + plnAmt + " WHERE C_Project_ID=" + GetC_Project_ID() , null , Get_Trx());
            }
            return success;
        }

        /**
         * 	Before Delete
         *	@return true
         */
        protected override bool BeforeDelete()
        {
            return Delete_Accounting("C_Project_Acct");
        }
    }
}
