using PX.Data.EP;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Data;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.CT;
using PX.Objects.EP;
using PX.Objects.GL;
using PX.Objects.PM;
using PX.Objects;
using PX.TM;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;

namespace PX.Objects.EP
{
      [PXNonInstantiatedExtension]
  public class EP_EPActivityApprove_ExistingColumn : PXCacheExtension<PX.Objects.EP.EPActivityApprove>
  {
      #region ProjectID  
      [PXMergeAttributes(Method = MergeMethod.Append)]
      [PXCustomizeSelectorColumns(
        typeof(PX.Objects.PM.PMProject.contractCD),
        typeof(PX.Objects.PM.PMProject.description),
        typeof(PX.Objects.PM.PMProject.customerID),
        typeof(PX.Objects.PM.PMProject.customerID_Customer_acctName),
        typeof(PX.Objects.PM.PMProject.status))]
      public int? ProjectID { get; set; }
      #endregion

      #region Date  
      [PXDBDateAndTime(DisplayNameDate = "Date", DisplayNameTime = "Time", UseTimeZone = true)]
      [PXUIField(DisplayName = "Date")]
      [PXDefault(typeof(AccessInfo.businessDate))]
      public DateTime? Date { get; set; }
      #endregion

      #region IsBillable  
      [PXDBBool]
      [PXUIField(DisplayName = "Billable", FieldClass = "BILLABLE")]
      [PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
      public bool? IsBillable { get; set; }
      #endregion      


  }
}