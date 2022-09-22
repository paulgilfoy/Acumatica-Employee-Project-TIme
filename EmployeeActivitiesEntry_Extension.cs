using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data.EP;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.CT;
using PX.Objects.IN;
using PX.Objects.PM;
using PX.SM;
using PX.Data;
using PX.TM;
using OwnedFilter = PX.Objects.CR.OwnedFilter;
using PX.Api;
using PX.Objects;

namespace PX.Objects.EP
{
    public class EmployeeActivitiesEntry_Extension : PXGraphExtension<EmployeeActivitiesEntry>
    {
        //Adding the Filter object for work in IEnumerable activity()
        public PXFilter<PX.Objects.EP.EmployeeActivitiesEntry.PMTimeActivityFilter> Filter;

        #region Event Handler
        //Copy and pasted IEnumerable activity() from base, adding if clause for   OwnedFilterExt.UsrPGDate   filter
		protected virtual IEnumerable activity()
		{

			List<object> args = new List<object>();
			PX.Objects.EP.EmployeeActivitiesEntry.PMTimeActivityFilter filterRow = Filter.Current;
            OwnedFilterExt ownedFilterExt = PXCache<OwnedFilter>.GetExtension<OwnedFilterExt>(filterRow);
            
			if (filterRow == null)
				return null;

			BqlCommand cmd;
			cmd = BqlCommand.CreateInstance(typeof(Select2<
				EPActivityApprove,
				LeftJoin<EPEarningType, 
					On<EPEarningType.typeCD, Equal<EPActivityApprove.earningTypeID>>,
				LeftJoin<CRActivityLink, 
					On<CRActivityLink.noteID, Equal<EPActivityApprove.refNoteID>>,
				LeftJoin<CRCase, 
					On<CRCase.noteID, Equal<CRActivityLink.refNoteID>>,
				LeftJoin<ContractEx, 
					On<CRCase.contractID, Equal<ContractEx.contractID>>>>>>,
				Where
					<EPActivityApprove.ownerID, Equal<Current<PX.Objects.EP.EmployeeActivitiesEntry.PMTimeActivityFilter.ownerID>>,
					And<EPActivityApprove.trackTime, Equal<True>,
                    And<EPActivityApprove.isCorrected, Equal<False>>>>,
				OrderBy<Desc<EPActivityApprove.date>>>));


			if (filterRow.ProjectID != null)
				cmd = cmd.WhereAnd<Where<EPActivityApprove.projectID, Equal<Current<PX.Objects.EP.EmployeeActivitiesEntry.PMTimeActivityFilter.projectID>>>>();

			if (filterRow.ProjectTaskID != null)
				cmd = cmd.WhereAnd<Where<EPActivityApprove.projectTaskID, Equal<Current<PX.Objects.EP.EmployeeActivitiesEntry.PMTimeActivityFilter.projectTaskID>>>>();

            if (ownedFilterExt.UsrPGFromDate != null)
            {
                PX.Data.PXTrace.WriteInformation(String.Format("Hello Paul. Please see the below contents. PS - You got this! Every place you set your foot, I have given you!    \nUsrPGFromDate = {0} \n PXTimeZoneInfo.Now.Date = {1}", ownedFilterExt.UsrPGFromDate, PX.Common.PXTimeZoneInfo.Now.Date));
                cmd = cmd.WhereAnd<Where<EPActivityApprove.date, GreaterEqual<Current<OwnedFilterExt.usrPGFromDate>>>>();
            }

            if (ownedFilterExt.UsrPGTillDate != null)
            {
                PX.Data.PXTrace.WriteInformation(String.Format("Hello Paul. Please see the below contents. PS - You got this! Every place you set your foot, I have given you!    \nUsrPGTillDate = {0} \n PXTimeZoneInfo.Now.Date = {1}", ownedFilterExt.UsrPGTillDate, PX.Common.PXTimeZoneInfo.Now.Date));
                cmd = cmd.WhereAnd<Where<EPActivityApprove.date, LessEqual<Current<OwnedFilterExt.usrPGTillDate>>>>();
            }


			// if (filterRow.FromWeek != null || filterRow.TillWeek != null)
			// {
			// 	List<Type> cmdList = new List<Type>();

			// 	if (filterRow.IncludeReject == true)
			// 	{
			// 		cmdList.Add(typeof(Where<,,>));
			// 		cmdList.Add(typeof(EPActivityApprove.approvalStatus));
			// 		cmdList.Add(typeof(Equal<CR.ActivityStatusListAttribute.rejected>));
			// 		cmdList.Add(typeof(Or<>));
			// 	}

			// 	if (filterRow.FromWeek != null)
			// 	{
			// 		if (filterRow.TillWeek != null)
			// 			cmdList.Add(typeof(Where<,,>));
			// 		else
			// 			cmdList.Add(typeof(Where<,>));
			// 		cmdList.Add(typeof(EPActivityApprove.weekID));
			// 		cmdList.Add(typeof(GreaterEqual<Required<PX.Objects.EP.EmployeeActivitiesEntry.PMTimeActivityFilter.fromWeek>>));
			// 		args.Add(filterRow.FromWeek);
			// 		if (filterRow.TillWeek != null)
			// 			cmdList.Add(typeof(And<>));
			// 	}

			// 	if (filterRow.TillWeek != null)
			// 	{
			// 		cmdList.Add(typeof(Where<EPActivityApprove.weekID, LessEqual<Required<PX.Objects.EP.EmployeeActivitiesEntry.PMTimeActivityFilter.tillWeek>>>));
			// 		args.Add(filterRow.TillWeek);
			// 	}

			// 	cmd = cmd.WhereAnd(BqlCommand.Compose(cmdList.ToArray()));
			// }

			if (filterRow.NoteID != null)
			{
				cmd = cmd.WhereAnd<Where<EPActivityApprove.noteID, Equal<Required<PX.Objects.EP.EmployeeActivitiesEntry.PMTimeActivityFilter.noteID>>>>();
				args.Add(filterRow.NoteID);
			}

			PXView view = new PXView(Base, false, cmd);
			return view.SelectMultiBound(new object[] { Filter.Current }, args.ToArray());
		}

        

        protected void EPActivityApprove_UsrPGProgressStartTime_FieldDefaulting(PXCache cache, PXFieldDefaultingEventArgs e)
        {
            EPActivityApprove row = (EPActivityApprove)e.Row;
            PMTimeActivityExt pMTimeActivityExt = PXCache<PMTimeActivity>.GetExtension<PMTimeActivityExt>(row);
            var k = DateTime.Now;
            if (row == null)
            {
                e.NewValue = PX.Common.PXTimeZoneInfo.Now;
            }
            else
            {
                e.NewValue = PX.Common.PXTimeZoneInfo.Now;
            }
      
        }

        protected void EPActivityApprove_UsrPGClockStatus_FieldDefaulting(PXCache cache, PXFieldDefaultingEventArgs e)
        {
        
            EPActivityApprove row = (EPActivityApprove)e.Row;
            PMTimeActivityExt pMTimeActivityExt = PXCache<PMTimeActivity>.GetExtension<PMTimeActivityExt>(row);
            if (row == null)
            {
                e.NewValue = "A";
            }
            else
            {
                e.NewValue = "A";
            }
        
        }

        protected virtual void EPActivityApprove_Date_FieldDefaulting(PXCache cache, PXFieldDefaultingEventArgs e)
        {
            EPActivityApprove row = (EPActivityApprove)e.Row;
            if (row == null)
            {
                row.Date = PX.Common.PXTimeZoneInfo.Now.Date;
                //row.Date = PX.Data.PXGraph.Current<AccessInfo.businessDate>;
            }
            else
            {
                row.Date = PX.Common.PXTimeZoneInfo.Now.Date;
                //row.Date = AccessInfo.BusinessDate;
            }
        }

        protected virtual void PMTimeActivityFilter_FromWeek_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			try
			{
				e.NewValue = PXWeekSelector2Attribute.GetWeekID(this, null);
			}
			catch (PXException exception)
			{
				sender.RaiseExceptionHandling<PX.Objects.EP.EmployeeActivitiesEntry.PMTimeActivityFilter.fromWeek>(e.Row, null, exception);
			}

		}

		protected virtual void PMTimeActivityFilter_TillWeek_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			try
			{
				e.NewValue = null;
			}
			catch (PXException exception)
			{
				sender.RaiseExceptionHandling<PX.Objects.EP.EmployeeActivitiesEntry.PMTimeActivityFilter.fromWeek>(e.Row, null, exception);
			}
		}

        #endregion

    
        #region Actions

        //Punch-in Punch-out time tracking  section
        
        public PXAction<PX.Objects.EP.EmployeeActivitiesEntry.PMTimeActivityFilter> Stop_Timer;

        [PXButton]
        [PXUIField(DisplayName = "Stop")]
        protected void stop_Timer()
        {
            EPActivityApprove row = Base.Activity.Current;
            PMTimeActivityExt pMTimeActivityExt = PXCache<PMTimeActivity>.GetExtension<PMTimeActivityExt>(row);
            var k = PX.Common.PXTimeZoneInfo.Now;
            if (row.ApprovalStatus != "OP" || pMTimeActivityExt.UsrPGClockStatus == "C")
            {
                throw new PXException(String.Format("Row selected is not valid. \nRow.Status = {0} \nRow.UsrPGClockStatus = {1}", row.ApprovalStatus, pMTimeActivityExt.UsrPGClockStatus));
            }
            else if (row.ApprovalStatus == "OP")
            {
                if (pMTimeActivityExt.UsrPGIsPaused == false)
                {
                    Base.Caches[typeof(PMTimeActivity)].SetValueExt<PMTimeActivityExt.usrPGProgressEndTime>(row, k);
                    Base.Caches[typeof(PMTimeActivity)].Update(pMTimeActivityExt);
                    if (pMTimeActivityExt.UsrPGProgressEndTime != null && pMTimeActivityExt.UsrPGProgressStartTime < pMTimeActivityExt.UsrPGProgressEndTime)
                        {
                            TimeSpan t = (TimeSpan)(pMTimeActivityExt.UsrPGProgressEndTime - pMTimeActivityExt.UsrPGProgressStartTime);
                            pMTimeActivityExt.UsrPGProgressTimeSpent = (int)t.TotalMinutes;
                        }
                    else
                        return;
                    row.TimeSpent = row.TimeSpent + pMTimeActivityExt.UsrPGProgressTimeSpent;
                }   
                else if (pMTimeActivityExt.UsrPGIsPaused == true)
                {
                    Base.Caches[typeof(PMTimeActivity)].SetValueExt<PMTimeActivityExt.usrPGIsPaused>(row, false);
                }

            }

            Base.Caches[typeof(PMTimeActivity)].SetValueExt<PMTimeActivityExt.usrPGEndDate>(row, k);
            Base.Caches[typeof(PMTimeActivity)].SetValueExt<PMTimeActivityExt.usrPGClockStatus>(row, "C");
            if (row.ProjectTaskID == null) {row.IsBillable = false;}
            else if (row.ProjectTaskID != null) 
            {row.IsBillable = true;
            row.TimeBillable = row.TimeSpent;}
            row.Hold = false;
            Base.Caches[typeof(PMTimeActivity)].Update(pMTimeActivityExt);
            Base.Caches[typeof(EPActivityApprove)].Update(row);
            Base.Save.Press();
            
        }



        public PXAction<PX.Objects.EP.EmployeeActivitiesEntry.PMTimeActivityFilter> Pause_Timer;

        [PXButton]
        [PXUIField(DisplayName = "Pause/Play")]
        protected void pause_Timer()
        {
            EPActivityApprove row = Base.Activity.Current;
            PMTimeActivityExt pMTimeActivityExt = PXCache<PMTimeActivity>.GetExtension<PMTimeActivityExt>(row);
            var k = PX.Common.PXTimeZoneInfo.Now;
            if (row.ApprovalStatus != "OP" || pMTimeActivityExt.UsrPGClockStatus == "C")
            {
                throw new PXException(String.Format("Row selected is not valid. \nRow.Status = {0} \nRow.UsrPGClockStatus = {1}", row.ApprovalStatus, pMTimeActivityExt.UsrPGClockStatus));
            }
            else if (row.ApprovalStatus == "OP")
            {
                Base.Caches[typeof(PMTimeActivity)].SetValueExt<PMTimeActivityExt.usrPGEndDate>(row, null);
                if (pMTimeActivityExt.UsrPGIsPaused == false)
                    {
                        Base.Caches[typeof(PMTimeActivity)].SetValueExt<PMTimeActivityExt.usrPGProgressEndTime>(row, k);
                        Base.Caches[typeof(PMTimeActivity)].Update(pMTimeActivityExt);
                        if (pMTimeActivityExt.UsrPGProgressEndTime != null && pMTimeActivityExt.UsrPGProgressStartTime < pMTimeActivityExt.UsrPGProgressEndTime)
                            {
                                TimeSpan t = (TimeSpan)(pMTimeActivityExt.UsrPGProgressEndTime - pMTimeActivityExt.UsrPGProgressStartTime);
                                pMTimeActivityExt.UsrPGProgressTimeSpent = (int)t.TotalMinutes;
                            }
                        else
                            return;
                        row.TimeSpent = row.TimeSpent + pMTimeActivityExt.UsrPGProgressTimeSpent;
                        Base.Caches[typeof(PMTimeActivity)].SetValueExt<PMTimeActivityExt.usrPGIsPaused>(row, true);
                        Base.Caches[typeof(PMTimeActivity)].SetValueExt<PMTimeActivityExt.usrPGClockStatus>(row, "P");
                    }   
                else if (pMTimeActivityExt.UsrPGIsPaused == true)
                    {
                        Base.Caches[typeof(PMTimeActivity)].SetValueExt<PMTimeActivityExt.usrPGIsPaused>(row, false);
                        Base.Caches[typeof(PMTimeActivity)].SetValueExt<PMTimeActivityExt.usrPGClockStatus>(row, "A");
                        Base.Caches[typeof(PMTimeActivity)].SetValueExt<PMTimeActivityExt.usrPGProgressStartTime>(row, k);
                        Base.Caches[typeof(PMTimeActivity)].SetValueExt<PMTimeActivityExt.usrPGProgressEndTime>(row, null);
                        Base.Caches[typeof(PMTimeActivity)].SetValueExt<PMTimeActivityExt.usrPGProgressTimeSpent>(row, null);
                        
                        
                    }
                Base.Caches[typeof(PMTimeActivity)].Update(pMTimeActivityExt);
                Base.Caches[typeof(EPActivityApprove)].Update(row);      
                Base.Save.Press();          
            }

        }

        #endregion
    }
}