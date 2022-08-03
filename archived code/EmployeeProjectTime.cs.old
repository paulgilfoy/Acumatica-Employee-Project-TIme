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
        #region Event Handlers

        //public SelectFrom<EPActivityApprove>
        //            .InnerJoin<PMTask>
        //                .On<EPActivityApprove.projectTaskID.IsEqual<PMTask.taskID>>
        //            .LeftJoin<PMTimeActivity>
        //                .On<EPActivityApprove.origNoteID.IsEqual<PMTimeActivity.noteID>>
        //            .Where<EPActivityApprove.ownerID.IsEqual<EmployeeActivitiesEntry.PMTimeActivityFilter.ownerID.FromCurrent>>
        //            .View Activity;    

        public PXSelectJoin<EPActivityApprove,
                    InnerJoin<PMTask,
                            On<PMTask.taskID, Equal<EPActivityApprove.projectTaskID>>,
                    LeftJoin<PMTimeActivity,
                            On<PMTimeActivity.noteID, Equal<EPActivityApprove.origNoteID>>>>,
                    Where<EPActivityApprove.ownerID, Equal<Current<EmployeeActivitiesEntry.PMTimeActivityFilter.ownerID>>,
                            And<EPActivityApprove.trackTime, Equal<True>>>> Activity;


        public PXAction<PX.Objects.EP.EmployeeActivitiesEntry.PMTimeActivityFilter> Stop_Timer;

        [PXButton]
        [PXUIField(DisplayName = "Stop Timer")]
        protected void stop_Timer()
        {
            EPActivityApprove row = Activity.Current;
            PMTimeActivityExt pMTimeActivityExt = PXCache<PMTimeActivity>.GetExtension<PMTimeActivityExt>(row);
            var k = DateTime.Now;
            Base.Caches[typeof(PMTimeActivity)].SetValueExt<PMTimeActivityExt.usrEndTime>(row, k);
            Base.Caches[typeof(PMTimeActivity)].Update(pMTimeActivityExt);
            //row.UsrEndTime = DateTime.Now;
            //row.Update(Activity);
            if (pMTimeActivityExt.UsrEndTime != null && row.Date < pMTimeActivityExt.UsrEndTime)
            {
                TimeSpan t = (TimeSpan)(pMTimeActivityExt.UsrEndTime - row.Date);
                row.TimeSpent = (int)t.TotalMinutes;
            }
            else
                return;
        }


        protected virtual void EPActivityApprove_Date_FieldDefaulting(PXCache cache, PXFieldDefaultingEventArgs e)
        {
            EPActivityApprove row = (EPActivityApprove)e.Row;
            if (row == null)
            {
                row.Date = DateTime.Now;
            }
            else
            {
                row.Date = DateTime.Now;
            }
        }

        #endregion
    }
}