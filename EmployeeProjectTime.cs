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
        #region Actions

        public PXAction<PX.Objects.EP.EmployeeActivitiesEntry.PMTimeActivityFilter> Stop_Timer;

        [PXButton]
        [PXUIField(DisplayName = "Stop")]
        protected void stop_Timer()
        {
            EPActivityApprove row = Base.Activity.Current;
            PMTimeActivityExt pMTimeActivityExt = PXCache<PMTimeActivity>.GetExtension<PMTimeActivityExt>(row);
            var k = DateTime.Now;
            if (row.ApprovalStatus != "OP")
            {
                throw new PXException("Row selected is not valid. Row.Status = " + (string)row.ApprovalStatus);
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
            row.IsBillable = true;
            row.TimeBillable = row.TimeSpent;
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
            var k = DateTime.Now;
            if (row.ApprovalStatus != "OP")
            {
                throw new PXException("Row selected is not valid. Row.Status = " + (string)row.ApprovalStatus);
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
                    }   
                else if (pMTimeActivityExt.UsrPGIsPaused == true)
                    {
                        Base.Caches[typeof(PMTimeActivity)].SetValueExt<PMTimeActivityExt.usrPGIsPaused>(row, false);
                        Base.Caches[typeof(PMTimeActivity)].SetValueExt<PMTimeActivityExt.usrPGProgressStartTime>(row, k);
                        Base.Caches[typeof(PMTimeActivity)].SetValueExt<PMTimeActivityExt.usrPGProgressEndTime>(row, null);
                        Base.Caches[typeof(PMTimeActivity)].SetValueExt<PMTimeActivityExt.usrPGProgressTimeSpent>(row, null);
                        
                        
                    }
                Base.Caches[typeof(PMTimeActivity)].Update(pMTimeActivityExt);
                Base.Caches[typeof(EPActivityApprove)].Update(row);                
            }

        }

        #endregion
    }
}