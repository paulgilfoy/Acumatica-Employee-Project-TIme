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
        
        //Modifying the filter object.      
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
                    cmd = cmd.WhereAnd<Where<EPActivityApprove.date, GreaterEqual<Current<OwnedFilterExt.usrPGFromDate>>>>();
                }

                if (ownedFilterExt.UsrPGTillDate != null)
                {
                    cmd = cmd.WhereAnd<Where<EPActivityApprove.date, LessEqual<Current<OwnedFilterExt.usrPGTillDate>>>>();
                }


        // if (filterRow.FromWeek != null || filterRow.TillWeek != null)
        // {
        //   List<Type> cmdList = new List<Type>();

        //   if (filterRow.IncludeReject == true)
        //   {
        //     cmdList.Add(typeof(Where<,,>));
        //     cmdList.Add(typeof(EPActivityApprove.approvalStatus));
        //     cmdList.Add(typeof(Equal<CR.ActivityStatusListAttribute.rejected>));
        //     cmdList.Add(typeof(Or<>));
        //   }

        //   if (filterRow.FromWeek != null)
        //   {
        //     if (filterRow.TillWeek != null)
        //       cmdList.Add(typeof(Where<,,>));
        //     else
        //       cmdList.Add(typeof(Where<,>));
        //     cmdList.Add(typeof(EPActivityApprove.weekID));
        //     cmdList.Add(typeof(GreaterEqual<Required<PX.Objects.EP.EmployeeActivitiesEntry.PMTimeActivityFilter.fromWeek>>));
        //     args.Add(filterRow.FromWeek);
        //     if (filterRow.TillWeek != null)
        //       cmdList.Add(typeof(And<>));
        //   }

        //   if (filterRow.TillWeek != null)
        //   {
        //     cmdList.Add(typeof(Where<EPActivityApprove.weekID, LessEqual<Required<PX.Objects.EP.EmployeeActivitiesEntry.PMTimeActivityFilter.tillWeek>>>));
        //     args.Add(filterRow.TillWeek);
        //   }

        //   cmd = cmd.WhereAnd(BqlCommand.Compose(cmdList.ToArray()));
        // }

        if (filterRow.NoteID != null)
        {
            cmd = cmd.WhereAnd<Where<EPActivityApprove.noteID, Equal<Required<PX.Objects.EP.EmployeeActivitiesEntry.PMTimeActivityFilter.noteID>>>>();
            args.Add(filterRow.NoteID);
        }

        PXView view = new PXView(Base, false, cmd);
        return view.SelectMultiBound(new object[] { Filter.Current }, args.ToArray());
        }

        //starting the time tracking clock 
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
        //setting the status of the time tracking clock. 
        //Options include A = Active, P = Paused, C = Completed
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

        //setting field defaulting to Dallas time zone. Now().
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

        #endregion

    
        #region Actions

        //Actions for Punch-in / Punch-out time tracking 
        
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
                        {throw new PXException(String.Format("A unique error has occured. Please contact Paul or system Admin. Error at Line 200"));
                        return;}
                    row.TimeSpent = row.TimeSpent + pMTimeActivityExt.UsrPGProgressTimeSpent;
                }   
                else if (pMTimeActivityExt.UsrPGIsPaused == true)
                {
                    Base.Caches[typeof(PMTimeActivity)].SetValueExt<PMTimeActivityExt.usrPGIsPaused>(row, false);
                }

            }

            Base.Caches[typeof(PMTimeActivity)].SetValueExt<PMTimeActivityExt.usrPGEndDate>(row, k);
            Base.Caches[typeof(PMTimeActivity)].SetValueExt<PMTimeActivityExt.usrPGClockStatus>(row, "C");
            //Below is a test to see if the row (time entry) is NON Billable
            //Three (3) tests. (1) If the Project Task ID is blank. (2) If the Project ID is "2023-FIRM-FIRM.001". Or (3) If the Labor Item is "NonBill" 
            if (row.ProjectTaskID == null || row.ProjectID == 5425 || row.LabourItemID == 10892) {row.IsBillable = false;}
            //if (row.ProjectTaskID == null || row.ProjectID == "2023FIRM        FIRM.001") {row.IsBillable = false;}
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
                            {throw new PXException(String.Format("A unique error has occured. Please contact Paul or system Admin. Error at Line 252"));
                            return;}
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