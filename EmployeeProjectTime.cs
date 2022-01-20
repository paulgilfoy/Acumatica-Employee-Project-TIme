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
using PX.Objects.EP;

namespace PX.Objects.EP
{
  public class EmployeeActivitiesEntry_Extension : PXGraphExtension<EmployeeActivitiesEntry>
  {
    #region Event Handlers

    // Adding an Action to Start / Stop Timer
    // public PXAction<PX.Objects.EP.EmployeeActivitiesEntry.PMTimeActivityFilter> Stop Timer;
  
    // [PXButton(CommitChanges = true)]
    // [PXUIField(DisplayName = "Stop Timer")]
    // protected void stop Timer()
    // {
    //   EPActivityApprove row = EPActivityApprove.Row;
    //   if (row == null)
    //   return;
    //   if 

    // }


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


    protected virtual void EPActivityApprove_TimeSpent_FieldUpdated(PXCache cache, PXFieldUpdatedEventArgs e)
		{
			EPActivityApprove row = (EPActivityApprove)e.Row;
			if (row == null)
				return;
			if (PMTimeActivityExt.UsrEndTime != null && row.Date < DateTime.Parse(PMTimeActivityExt.UsrEndTime))
        {
          row.TimeSpent = DateTime.Parse(PMTimeActivityExt.UsrEndTime) - row.Date;
        }
      else
				row.TimeBillable = GetTimeBillable(row, (int?)e.OldValue);
		}
    
    protected virtual int? GetTimeBillable(EPActivityApprove row, int? OldTimeSpent)
		{
			if (row.TimeCardCD == null && row.Billed != true)
			{
				if (row.IsBillable != true)
					return 0;
				else if ((OldTimeSpent ?? 0) == 0 || OldTimeSpent == row.TimeBillable)
					return row.TimeSpent;
				else
					return row.TimeBillable;
			}
			else
				return row.TimeBillable;
		}

    #endregion
  }
}