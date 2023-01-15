#IsBillable
[PXDBBool]
[PXUIField(DisplayName = "Billable", FieldClass = "BILLABLE")]
[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
#endregion

#UsrPGClockStatus
//Storage Type:  DBTableColumn
//BQL Field:    PMTimeActivityExt.usrPGClockStatus
[PXDBString(2)]
[PXStringList(new string[] {"A", "P", "C"}, new string[] {"Active", "Paused", "Completed"})]
[PXUIField(DisplayName="Active Clocks")]
#endregion

#UsrPGEndDate
//Storage Type:  DBTableColumn
//BQL Field:    PMTimeActivityExt.usrPGEndDate
[PXDBDate]
[PXUIField(DisplayName="End Date")]
#endregion

#UsrPGIsPaused
//Storage Type:  DBTableColumn
//BQL Field:    PMTimeActivityExt.usrPGIsPaused
[PXDBBool]
[PXUIField(DisplayName="Paused")]
[PXDefault(false)]
#endregion

#UsrPGProgressEndTime
//Storage Type:  DBTableColumn
//BQL Field:    PMTimeActivityExt.usrPGProgressEndTime
[PXDBDateAndTime(DisplayNameDate = "Progress End Date", DisplayNameTime = "Progress End Time", UseTimeZone = true)]
[PXUIField(DisplayName="Progress End")]
#endregion

#UsrPGProgressStartTime
//Storage Type:  DBTableColumn
//BQL Field:    PMTimeActivityExt.usrPGProgressStartTime
[PXDBDateAndTime(DisplayNameDate = "Progress Start Date", DisplayNameTime = "Progress Start Time", UseTimeZone = true)]
[PXUIField(DisplayName="Progress Start")]
[PXDefault(typeof(AccessInfo.businessDate))]
#endregion

#UsrPGProgressTimeSpent
//Storage Type:  DBTableColumn
//BQL Field:    PMTimeActivityExt.usrPGProgressTimeSpent
[PXDBInt]
[PXUIField(DisplayName="Progress Time Spent")]
#endregion