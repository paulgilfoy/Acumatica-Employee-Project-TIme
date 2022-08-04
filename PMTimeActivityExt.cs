using PX.Data.BQL.Fluent;
using PX.Data.EP;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Data;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.CT;
using PX.Objects.EP;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.PM;
using PX.Objects;
using PX.SM;
using PX.TM;
using PX.Web.UI;
using System.Collections.Generic;
using System;

namespace PX.Objects.CR
{
  public class PMTimeActivityExt : PXCacheExtension<PX.Objects.CR.PMTimeActivity>
  {
    #region UsrPGEndDate
    [PXDBDate]
    [PXUIField(DisplayName="End Date")]
    public virtual DateTime? UsrPGEndDate { get; set; }
    public abstract class usrPGEndDate : PX.Data.BQL.BqlString.Field<usrPGEndDate> { }
    #endregion

    #region UsrPGIsPaused
    [PXDBBool]
    [PXUIField(DisplayName="Paused")]
    [PXDefault(false)]
    public virtual bool? UsrPGIsPaused { get; set; }
    public abstract class usrPGIsPaused : PX.Data.BQL.BqlString.Field<usrPGIsPaused> { }
    #endregion

    #region UsrPGProgressEndTime
    [PXDBDateAndTime(DisplayNameDate = "Progress End Date", DisplayNameTime = "Progress End Time", UseTimeZone = true)]
    [PXUIField(DisplayName="Progress End")]
    public virtual DateTime? UsrPGProgressEndTime { get; set; }
    public abstract class usrPGProgressEndTime : PX.Data.BQL.BqlString.Field<usrPGProgressEndTime> { }
    #endregion

    #region UsrPGProgressStartTime
    [PXDBDateAndTime(DisplayNameDate = "Progress Start Date", DisplayNameTime = "Progress Start Time", UseTimeZone = true)]
    [PXUIField(DisplayName="Progress Start")]
    public virtual DateTime? UsrPGProgressStartTime 
      { 
        get
        {
          return UsrPGProgressStartTime;  
        } 
        set
        {
          UsrPGProgressStartTime = DateTime.Now; 
          this.UsrPGProgressStartTime = value;
        } 
      }
    public abstract class usrPGProgressStartTime : PX.Data.BQL.BqlString.Field<usrPGProgressStartTime> { }
    #endregion

    #region UsrPGProgressTimeSpent
    [PXDBInt]
    [PXUIField(DisplayName="Progress Time Spent")]
    public virtual int? UsrPGProgressTimeSpent { get; set; }
    public abstract class usrPGProgressTimeSpent : PX.Data.BQL.BqlString.Field<usrPGProgressTimeSpent> { }
    #endregion
      
  }
}
