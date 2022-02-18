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
    public PXAction<PX.Objects.EP.EmployeeActivitiesEntry.PMTimeActivityFilter> Stop_Timer;
    [PXButton(CommitChanges = true)]
    [PXUIField(DisplayName = "Stop Timer")]
    public virtual void _(Events.RowSelected<EmployeeActivitiesEntry.EmployeeActivitiesEntry> e)
      {
        EPActivityApprove row = (EPActivityApprove)e.Row;
        //TimeSpan t = (TimeSpan)(row.Date - DateTime.Now);
        //int i = (int) t.TotalMinutes;
        //PX.Objects.CR.PMTimeActivityExt.usrEndTime myObj = new PX.Objects.CR.PMTimeActivityExt.usrEndTime
        //{set : (int?) t.TotalMinutes};
        PX.Objects.CR.PMTimeActivityExt myObj = new PX.Objects.CR.PMTimeActivityExt();
        myObj.UsrEndTime = DateTime.Now;
        //if (row == null)
        // return;
        //if (row != null)
          
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


//    protected virtual void EPActivityApprove_TimeSpent_FieldUpdated(PXCache cache, PXFieldUpdatedEventArgs e)
//    {
//      EPActivityApprove row = (EPActivityApprove)e.Row;
//      if (row == null)
//        return;
//      if (row.UsrEndTime_Date != null && row.Date < DateTime.Parse(row.UsrEndTime_Time))
//        {
//          row.TimeSpent = DateTime.Parse(row.UsrEndTime) - row.Date;
//        }
//      else
//        row.TimeBillable = GetTimeBillable(row, (int?)e.OldValue);
//    }


    #endregion
  }
}