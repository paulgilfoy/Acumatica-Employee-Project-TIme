

public class EPActivityApprove : PMTimeActivity
	{
		#region Overrides

		public new abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }
		public new abstract class parentTaskNoteID : PX.Data.BQL.BqlGuid.Field<parentTaskNoteID> { }
		public new abstract class summary : PX.Data.BQL.BqlString.Field<summary> { }
		public new abstract class timeSpent : PX.Data.BQL.BqlInt.Field<timeSpent> { }
		public new abstract class timeBillable : PX.Data.BQL.BqlInt.Field<timeBillable> { }
		public new abstract class ownerID : PX.Data.BQL.BqlInt.Field<ownerID> { }
		public new abstract class workgroupID : PX.Data.BQL.BqlInt.Field<workgroupID> { }
		#endregion

		#region TrackTime

		public new abstract class trackTime : PX.Data.BQL.BqlBool.Field<trackTime> { }

		[PXDBBool]
		[PXDefault(true)]
		public override bool? TrackTime { get; set; }
		#endregion

		#region Approve

		public abstract class isApproved : PX.Data.BQL.BqlBool.Field<isApproved> { }
		protected bool? _IsApproved;
		[PXBool]
		[PXUIField(DisplayName = "Approve")]
		public virtual bool? IsApproved
		{
			get
			{
				return _IsApproved ?? ApprovalStatus == ActivityStatusListAttribute.Approved;
			}
			set
			{
				_IsApproved = value;
				if (_IsApproved == true)
					_IsReject = false;
			}
		}
		#endregion

		#region IsReject

		public abstract class isReject : PX.Data.BQL.BqlBool.Field<isReject> { }
		protected bool? _IsReject;
		[PXBool]
		[PXUIField(DisplayName = "Reject")]
		public virtual bool? IsReject
		{
			get
			{
				return _IsReject ?? ApprovalStatus == ActivityStatusListAttribute.Rejected;
			}
			set
			{
				_IsReject = value;
				if (_IsReject == true)
					_IsApproved = false;
			}
		}

		#endregion
		
		#region ContractID

		public new abstract class contractID : PX.Data.BQL.BqlInt.Field<contractID> { }

		[PXDBInt(BqlField = typeof(PMTimeActivity.contractID))]
		[PXUIField(DisplayName = "Contract", Visible = false)]
		[PXSelector(typeof(Search2<ContractExEx.contractID,
				LeftJoin<ContractBillingSchedule, On<ContractExEx.contractID, Equal<ContractBillingSchedule.contractID>>>,
			Where<ContractExEx.baseType, Equal<CTPRType.contract>>,
			OrderBy<Desc<ContractExEx.contractCD>>>),
			DescriptionField = typeof(ContractExEx.description),
			SubstituteKey = typeof(ContractExEx.contractCD), Filterable = true)]
		[PXRestrictor(typeof(Where<ContractExEx.status, Equal<Contract.status.active>>), CR.Messages.ContractIsNotActive)]
		[PXRestrictor(typeof(Where<Current<AccessInfo.businessDate>, LessEqual<ContractExEx.graceDate>, Or<Contract.expireDate, IsNull>>), CR.Messages.ContractExpired)]
		[PXRestrictor(typeof(Where<Current<AccessInfo.businessDate>, GreaterEqual<ContractExEx.startDate>>), CR.Messages.ContractActivationDateInFuture, typeof(ContractExEx.startDate))]
		public override Int32? ContractID { set; get; }
        #endregion

        #region ProjectID

        public new abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID> { }

        [EPActivityProjectDefault(typeof(isBillable))]
        [EPProject(typeof(ownerID), FieldClass = ProjectAttribute.DimensionName, BqlField = typeof(PMTimeActivity.projectID))]
        [PXFormula(typeof(
            Switch<
                Case<Where<Not<FeatureInstalled<FeaturesSet.projectModule>>>, DefaultValue<projectID>,
                Case<Where<isBillable, Equal<True>, And<Current2<projectID>, Equal<NonProject>>>, Null,
                Case<Where<isBillable, Equal<False>, And<Current2<projectID>, IsNull>>, DefaultValue<projectID>>>>,
            projectID>))]
        public override Int32? ProjectID { get; set; }
        #endregion

        #region ProjectTaskID

        public new abstract class projectTaskID : PX.Data.BQL.BqlInt.Field<projectTaskID> { }
        [PXDefault(typeof(Search<PMTask.taskID, Where<PMTask.projectID, Equal<Current<projectID>>, And<PMTask.isDefault, Equal<True>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
        [ProjectTask(typeof(projectID), BatchModule.TA, DisplayName = "Project Task")]
        [PXFormula(typeof(Switch<
            Case<Where<Current2<projectID>, Equal<NonProject>>, Null>,
            projectTaskID>))]
		[PXForeignReference(typeof(Field<projectTaskID>.IsRelatedTo<PMTask.taskID>))]

        /// <summary>
        /// Identifier of the <see cref="PX.Objects.PMTask.TaskID">TaskID</see>.
        /// </summary>
        public override int? ProjectTaskID { get; set; }
        #endregion

        #region Date

        public new abstract class date : PX.Data.BQL.BqlDateTime.Field<date> { }		
		[PXDBDateAndTime(BqlField = typeof(PMTimeActivity.date), DisplayNameDate = "Date", DisplayNameTime = "Time")]
		[PXUIField(DisplayName = "Date")]
		public override DateTime? Date { get; set; }
		#endregion

		#region WeekID

		public new abstract class weekID : PX.Data.BQL.BqlInt.Field<weekID> { }

		[PXDBInt(BqlField = typeof(PMTimeActivity.weekID))]
		[PXUIField(DisplayName = "Time Card Week", Enabled = false)]
		[PXWeekSelector2()]
		[PXFormula(typeof(Default<date, trackTime>))]
		[EPActivityDefaultWeek(typeof(date))]
		public override int? WeekID { get; set; }
		#endregion
		
		#region TimeCardCD

		public new abstract class timeCardCD : PX.Data.BQL.BqlString.Field<timeCardCD> { }

		[PXDBString(10, BqlField = typeof(PMTimeActivity.timeCardCD))]
		[PXUIField(DisplayName = "Time Card Ref.", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		public override string TimeCardCD { get; set; }
		#endregion

		#region ApprovalStatus

		public new abstract class approvalStatus : PX.Data.BQL.BqlString.Field<approvalStatus> { }

		[PXDBString(2, IsFixed = true, BqlField = typeof(PMTimeActivity.approvalStatus))]
		[ActivityStatusList]
		[PXUIField(DisplayName = "Status")]
		[PXDefault(ActivityStatusAttribute.Open, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXFormula(typeof(Switch<
			Case<Where<hold, IsNull, Or<hold, Equal<True>>>, ActivityStatusAttribute.open,			
			Case<Where<released, Equal<True>>, ActivityStatusAttribute.released,
			Case<Where<approverID, IsNotNull, And<hold, Equal<False>>>, 
				ActivityStatusAttribute.pendingApproval>>>,
			ActivityStatusAttribute.completed>))]
		public override string ApprovalStatus { get; set; }
		#endregion

		#region ApproverID

		public new abstract class approverID : PX.Data.BQL.BqlInt.Field<approverID> { }

		[PXDBInt(BqlField = typeof(PMTimeActivity.approverID))]
		[PXSelector(typeof(Search<EPEmployee.bAccountID>), SubstituteKey = typeof(EPEmployee.acctCD))]
		[PXFormula(typeof(
			Switch<
				Case<Where<Selector<projectTaskID, PMTask.approverID>, IsNotNull>, Selector<projectTaskID, PMTask.approverID>>, 
				Null>
			))]
		[PXUIField(DisplayName = "Approver", Visibility = PXUIVisibility.SelectorVisible)]
		public override Int32? ApproverID { get; set; }

		#endregion
		
		#region Hold

		public abstract class hold : PX.Data.BQL.BqlBool.Field<hold> { }

		[PXBool]
		[PXUIField(FieldName = "Hold", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual bool? Hold { get; set; }
		#endregion

		public new abstract class overtimeSpent : PX.Data.BQL.BqlInt.Field<overtimeSpent> { }
		public new abstract class overtimeBillable : PX.Data.BQL.BqlInt.Field<overtimeBillable> { }
	}


//**************************************

public partial class PMTimeActivity : IBqlTable
	{

		#region Keys


		/// <summary>
		/// Primary Key
		/// </summary>
		public class PK : PrimaryKeyOf<PMTimeActivity>.By<noteID>
		{
			public static PMTimeActivity Find(PXGraph graph, Guid? noteID) => FindBy(graph, noteID);
		}

		/// <summary>
		/// Foreign Keys
		/// </summary>
		public static class FK
		{
			/// <summary>
			/// Time Card
			/// </summary>
			public class Timecard : EPTimeCard.PK.ForeignKeyOf<PMTimeActivity>.By<timeCardCD> { }

			/// <summary>
			/// Earning Type
			/// </summary>
			public class EarningType : EPEarningType.PK.ForeignKeyOf<PMTimeActivity>.By<earningTypeID> { }

			/// <summary>
			/// Owner
			/// </summary>
			public class OwnerContact : Contact.PK.ForeignKeyOf<PMTimeActivity>.By<ownerID> { }

			/// <summary>
			/// Project
			/// </summary>
			public class Project : PMProject.PK.ForeignKeyOf<PMTimeActivity>.By<projectID> { }

			/// <summary>
			/// Project Task
			/// </summary>
			public class ProjectTask : PMTask.PK.ForeignKeyOf<PMTimeActivity>.By<projectTaskID> { }

			/// <summary>
			/// Cost Code
			/// </summary>
			public class CostCode : PMCostCode.PK.ForeignKeyOf<PMTimeActivity>.By<costCodeID> { }

			/// <summary>
			/// Related Activity.
			/// </summary>
			public class Related : CRActivity.PK.ForeignKeyOf<PMTimeActivity>.By<refNoteID> { }

			/// <summary>
			/// Parent Activity.
			/// </summary>
			public class Parent : CRActivity.PK.ForeignKeyOf<PMTimeActivity>.By<parentTaskNoteID> { }

			/// <summary>
			/// Union
			/// </summary>
			public class Union : PMUnion.PK.ForeignKeyOf<PMTimeActivity>.By<unionID> { }

			/// <summary>
			/// Work Code
			/// </summary>
			public class WorkCode : PMWorkCode.PK.ForeignKeyOf<PMTimeActivity>.By<workCodeID> { }

			/// <summary>
			/// Contract
			/// </summary>
			public class Contract : CT.Contract.PK.ForeignKeyOf<PMTimeActivity>.By<contractID> { }

			/// <summary>
			/// Approver
			/// </summary>
			public class Approver : EPEmployee.PK.ForeignKeyOf<PMTimeActivity>.By<approverID> { }

			/// <summary>
			/// Original/Corrected Acivity
			/// </summary>
			public class OriginalActivity : PMTimeActivity.PK.ForeignKeyOf<PMTimeActivity>.By<origNoteID> { }

			/// <summary>
			/// Labor Item
			/// </summary>
			public class LaborItem : InventoryItem.PK.ForeignKeyOf<PMTimeActivity>.By<labourItemID> { }

			/// <summary>
			/// Overtime Labor Item
			/// </summary>
			public class OvertimeItem : InventoryItem.PK.ForeignKeyOf<PMTimeActivity>.By<overtimeItemID> { }

			/// <summary>
			/// Shift Code
			/// </summary>
			public class ShiftCode : EPShiftCode.PK.ForeignKeyOf<PMTimeActivity>.By<shiftID> { }
		}
		#endregion

		#region Selected

		public abstract class selected : PX.Data.BQL.BqlBool.Field<selected> { }

		[PXBool]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Selected")]
		public virtual bool? Selected { get; set; }
		#endregion

		#region NoteID

		public abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }
		
		[PXDBGuid(true, IsKey = true)]
		public virtual Guid? NoteID { get; set; }
		#endregion

		#region RefNoteID

		public abstract class refNoteID : PX.Data.BQL.BqlGuid.Field<refNoteID> { }

		[PXSequentialSelfRefNote(new Type[0], SuppressActivitiesCount = true, NoteField = typeof(noteID))]
		[PXUIField(Visible = false)]
		[PXParent(typeof(Select<CRActivity, Where<CRActivity.noteID, Equal<Current<refNoteID>>>>), ParentCreate = true)]
		public virtual Guid? RefNoteID { get; set; }
		#endregion

		#region ParentTaskNoteID

		public abstract class parentTaskNoteID : PX.Data.BQL.BqlGuid.Field<parentTaskNoteID> { }

		[PXDBGuid]
		[PXDBDefault(null, PersistingCheck = PXPersistingCheck.Nothing)]		
        [CRTaskSelector]
		[PXRestrictor(typeof(Where<CRActivity.ownerID, Equal<Current<AccessInfo.contactID>>>), null)]
		[PXUIField(DisplayName = "Task")]
		public virtual Guid? ParentTaskNoteID { get; set; }
		#endregion

		#region TrackTime

		public abstract class trackTime : PX.Data.BQL.BqlBool.Field<trackTime> { }
        [PXDBBool]
        [PXDefault(false)]
        [PXUIField(DisplayName = "Track Time")]
        [PXFormula(typeof(IIf<
            Where<Current2<CRActivity.classID>, Equal<CRActivityClass.activity>, And<FeatureInstalled<FeaturesSet.timeReportingModule>>>,
            IsNull<Selector<Current<CRActivity.type>, EPActivityType.requireTimeByDefault>, False>, False>))]
        public virtual bool? TrackTime { get; set; }
        #endregion

        #region TimeCardCD

        public abstract class timeCardCD : PX.Data.BQL.BqlString.Field<timeCardCD> { }

		[PXDBString(10)]
		[PXUIField(Visible = false)]
		public virtual string TimeCardCD { get; set; }
		#endregion
		
		#region TimeSheetCD

		public abstract class timeSheetCD : PX.Data.BQL.BqlString.Field<timeSheetCD> { }

		[PXDBString(15)]
		[PXUIField(Visible = false)]
		public virtual string TimeSheetCD { get; set; }
		#endregion

		#region Summary

		public abstract class summary : PX.Data.BQL.BqlString.Field<summary> { }

		[PXDBString(Common.Constants.TranDescLength, InputMask = "", IsUnicode = true)]
		[PXDefault]
		[PXUIField(DisplayName = "Summary", Visibility = PXUIVisibility.SelectorVisible)]
		[PXFieldDescription]
		[PXNavigateSelector(typeof(summary))]
		public virtual string Summary { get; set; }
		#endregion

		#region Date

		public abstract class date : PX.Data.BQL.BqlDateTime.Field<date> { }

		[PXDBDateAndTime(DisplayNameDate = "Date", DisplayNameTime = "Time", UseTimeZone = true)]
		[PXUIField(DisplayName = "Date")]
		[PXFormula(typeof(IsNull<Current<CRActivity.startDate>, Current<CRSMEmail.startDate>>))]
		public virtual DateTime? Date { get; set; }
		#endregion

		#region DayOfWeek

		public abstract class dayOfWeek: PX.Data.BQL.BqlInt.Field<dayOfWeek> { }

		[PXInt(MaxValue = 6)]
		[PXUIField(DisplayName = "Day")]
		[PXDependsOnFields(typeof(date))]
		public virtual int? DayOfWeek => (int?)Date?.DayOfWeek;
		#endregion

		#region Owner

		public abstract class ownerID : PX.Data.BQL.BqlInt.Field<ownerID> { }

		[PXChildUpdatable(AutoRefresh = true)]
		[SubordinateOwnerEmployee]
		public virtual int? OwnerID { get; set; }
		#endregion
		
		#region EarningTypeID

		public abstract class earningTypeID : PX.Data.BQL.BqlString.Field<earningTypeID> { }

		[PXDBString(EPEarningType.typeCD.Length, IsUnicode = true, InputMask = EPEarningType.typeCD.InputMask)]
		[PXDefault("RG", typeof(Search<EPSetup.regularHoursType>), PersistingCheck = PXPersistingCheck.Null)]
		[PXRestrictor(typeof(Where<EPEarningType.isActive, Equal<True>>), EP.Messages.EarningTypeInactive, typeof(EPEarningType.typeCD))]
		[PXSelector(typeof(EPEarningType.typeCD), DescriptionField = typeof(EPEarningType.description))]
		[PXUIField(DisplayName = "Earning Type")]
		public virtual string EarningTypeID { get; set; }
		#endregion

		#region IsBillable

		public abstract class isBillable : PX.Data.BQL.BqlBool.Field<isBillable> { }

		[PXDBBool]
		[PXUIField(DisplayName = "Billable", FieldClass = "BILLABLE")]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXFormula(typeof(Switch<
			Case<Where<IsNull<Current<CRActivity.classID>, Current<CRSMEmail.classID>>, Equal<CRActivityClass.task>, 
				Or<IsNull<Current<CRActivity.classID>, Current<CRSMEmail.classID>>, Equal<CRActivityClass.events>>>, False,
			Case<Where2<FeatureInstalled<FeaturesSet.timeReportingModule>, And<trackTime, Equal<True>, And<earningTypeID, IsNotNull>>>,
				Selector<earningTypeID, EPEarningType.isbillable>>>,
			False>))]
		public virtual bool? IsBillable { get; set; }
		#endregion

		#region ProjectID

		public abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID> { }

		[EPActivityProjectDefault(typeof(isBillable))]
		[EPProject(typeof(ownerID), FieldClass = ProjectAttribute.DimensionName)]
		[PXFormula(typeof(
			Switch<
				Case<Where<Not<FeatureInstalled<FeaturesSet.projectModule>>>, DefaultValue<projectID>,
				Case<Where<isBillable, Equal<True>, And<Current2<projectID>, Equal<NonProject>>>, Null,
				Case<Where<isBillable, Equal<False>, And<Current2<projectID>, IsNull>>, DefaultValue<projectID>>>>,
			projectID>))]
		public virtual int? ProjectID { get; set; }
		#endregion

		#region ProjectTaskID

		public abstract class projectTaskID : PX.Data.BQL.BqlInt.Field<projectTaskID> { }
		[PXDefault(typeof(Search<PMTask.taskID, Where<PMTask.projectID, Equal<Current<projectID>>, And<PMTask.isDefault, Equal<True>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		[ProjectTask(typeof(projectID), BatchModule.TA, DisplayName = "Project Task")]
		[PXFormula(typeof(Switch<
			Case<Where<Current2<projectID>, Equal<NonProject>>, Null>,
			projectTaskID>))]
		[PXForeignReference(typeof(Field<projectTaskID>.IsRelatedTo<PMTask.taskID>))]
		public virtual int? ProjectTaskID { get; set; }
		#endregion

		#region CostCodeID

		public abstract class costCodeID : PX.Data.BQL.BqlInt.Field<costCodeID> { }
		[CostCode(null, typeof(projectTaskID), GL.AccountType.Expense, ReleasedField = typeof(released))]
		public virtual Int32? CostCodeID
		{
			get;
			set;
		}
		#endregion

		#region ExtRefNbr

		public abstract class extRefNbr : PX.Data.BQL.BqlString.Field<extRefNbr> { }
		protected String _ExtRefNbr;
		[PXDBString(30, IsUnicode = true)]
		[PXUIField(DisplayName = "External Ref. Nbr")]
		public virtual String ExtRefNbr
		{
			get
			{
				return this._ExtRefNbr;
			}
			set
			{
				this._ExtRefNbr = value;
			}
		}
		#endregion

		#region CertifiedJob

		public abstract class certifiedJob : PX.Data.BQL.BqlBool.Field<certifiedJob> { }
		[PXDBBool()]
		[PXDefault(typeof(Coalesce<Search<PMProject.certifiedJob, Where<PMProject.contractID, Equal<Current<projectID>>>>,
			Search<PMProject.certifiedJob, Where<PMProject.nonProject, Equal<True>>>>))]
		[PXUIField(DisplayName = "Certified Job", FieldClass = nameof(FeaturesSet.Construction))]
		public virtual Boolean? CertifiedJob
		{
			get; set;
		}
		#endregion

		#region UnionID

		public abstract class unionID : PX.Data.BQL.BqlString.Field<unionID> { }
		[PXForeignReference(typeof(Field<unionID>.IsRelatedTo<PMUnion.unionID>))]
		[PMUnion(typeof(projectID), typeof(Select<EPEmployee, Where<EPEmployee.defContactID, Equal<Current<ownerID>>>>))]
		public virtual String UnionID
		{
			get;
			set;
		}
		#endregion

		#region ApproverID

		public abstract class approverID : PX.Data.BQL.BqlInt.Field<approverID> { }

        [PXDBInt]
        [PXEPEmployeeSelector]
        [PXFormula(typeof(
            Switch<
                Case<Where<Current2<projectID>, Equal<NonProject>>, Null, Case<Where<Current2<projectTaskID>, IsNull>, Null>>,
                Selector<projectTaskID, PMTask.approverID>>
            ))]
        [PXUIField(DisplayName = "Approver", Visibility = PXUIVisibility.SelectorVisible)]
        public virtual int? ApproverID { get; set; }
        #endregion

        #region ApprovalStatus

        public abstract class approvalStatus : PX.Data.BQL.BqlString.Field<approvalStatus> { }

        [PXDBString(2, IsFixed = true)]
        [ApprovalStatus]
        [PXUIField(DisplayName = "Approval Status", Enabled = false)]
        [PXFormula(typeof(Switch<
            Case<Where<trackTime, Equal<True>, And<
                Where<Current2<approvalStatus>, IsNull, Or<Current2<approvalStatus>, Equal<ActivityStatusAttribute.open>>>>>, ActivityStatusAttribute.open,
            Case<Where<released, Equal<True>>, ActivityStatusAttribute.released,
            Case<Where<approverID, IsNotNull>, ActivityStatusAttribute.pendingApproval>>>,
            ActivityStatusAttribute.completed>))]
        public virtual string ApprovalStatus { get; set; }

        #endregion

        #region ApprovedDate

        public abstract class approvedDate : PX.Data.BQL.BqlDateTime.Field<approvedDate> { }

        [PXDBDate(DisplayMask = "d", PreserveTime = true)]
        [PXUIField(DisplayName = "Approved Date")]
        public virtual DateTime? ApprovedDate { get; set; }
		#endregion

		#region WorkgroupID

		public abstract class workgroupID : PX.Data.BQL.BqlInt.Field<workgroupID> { }
		[PXDBInt]
		[PXUIField(DisplayName = "Workgroup")]
		[PXWorkgroupSelector]
		[PXParent(typeof(Select<EPTimeActivitiesSummary, 
			Where<EPTimeActivitiesSummary.workgroupID, Equal<Current<workgroupID>>, 
				And<EPTimeActivitiesSummary.week, Equal<Current<weekID>>, 
				And<EPTimeActivitiesSummary.contactID, Equal<Current<ownerID>>>>>>),
			ParentCreate = true,
			LeaveChildren = true)]
		[PXDefault(typeof(SearchFor<EPEmployee.defaultWorkgroupID>
			.Where<EPEmployee.defContactID.IsEqual<PMTimeActivity.ownerID.FromCurrent>>), PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual int? WorkgroupID { get; set; }
		#endregion

		#region ContractID

		public abstract class contractID : PX.Data.BQL.BqlInt.Field<contractID> { }

		[PXDBInt]
		[PXUIField(DisplayName = "Contract", Visible = false)]
		[PXSelector(typeof(Search2<Contract.contractID,
			LeftJoin<ContractBillingSchedule, On<Contract.contractID, Equal<ContractBillingSchedule.contractID>>>,
			Where<Contract.baseType, Equal<CTPRType.contract>>,
			OrderBy<Desc<Contract.contractCD>>>),
			DescriptionField = typeof(Contract.description),
			SubstituteKey = typeof(Contract.contractCD), Filterable = true)]
		[PXRestrictor(typeof(Where<Contract.status, Equal<Contract.status.active>>), Messages.ContractIsNotActive)]
		[PXRestrictor(typeof(Where<Current<AccessInfo.businessDate>, LessEqual<Contract.graceDate>, Or<Contract.expireDate, IsNull>>), Messages.ContractExpired)]
		[PXRestrictor(typeof(Where<Current<AccessInfo.businessDate>, GreaterEqual<Contract.startDate>>), Messages.ContractActivationDateInFuture, typeof(Contract.startDate))]
		public virtual int? ContractID { get; set; }
		#endregion

		#region TimeSpent

		public abstract class timeSpent : PX.Data.BQL.BqlInt.Field<timeSpent> { }

		[PXDBInt]
		[PXTimeList]
		[PXDefault(0, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Time Spent")]
		[PXUnboundFormula(typeof(Switch<Case<Where<Selector<earningTypeID, EPEarningType.isOvertime>, Equal<False>>, timeSpent>, int0>), typeof(SumCalc<EPTimeActivitiesSummary.totalRegularTime>))]
		[PXUnboundFormula(typeof(timeSpent.When<dayOfWeek.IsEqual<int0>>.Else<int0>), typeof(SumCalc<EPTimeActivitiesSummary.sundayTime>))]
		[PXUnboundFormula(typeof(timeSpent.When<dayOfWeek.IsEqual<int1>>.Else<int0>), typeof(SumCalc<EPTimeActivitiesSummary.mondayTime>))]
		[PXUnboundFormula(typeof(timeSpent.When<dayOfWeek.IsEqual<int2>>.Else<int0>), typeof(SumCalc<EPTimeActivitiesSummary.tuesdayTime>))]
		[PXUnboundFormula(typeof(timeSpent.When<dayOfWeek.IsEqual<int3>>.Else<int0>), typeof(SumCalc<EPTimeActivitiesSummary.wednesdayTime>))]
		[PXUnboundFormula(typeof(timeSpent.When<dayOfWeek.IsEqual<int4>>.Else<int0>), typeof(SumCalc<EPTimeActivitiesSummary.thursdayTime>))]
		[PXUnboundFormula(typeof(timeSpent.When<dayOfWeek.IsEqual<int5>>.Else<int0>), typeof(SumCalc<EPTimeActivitiesSummary.fridayTime>))]
		[PXUnboundFormula(typeof(timeSpent.When<dayOfWeek.IsEqual<int6>>.Else<int0>), typeof(SumCalc<EPTimeActivitiesSummary.saturdayTime>))]
		public virtual int? TimeSpent { get; set; }
		#endregion

		#region OvertimeSpent

		public abstract class overtimeSpent : PX.Data.BQL.BqlInt.Field<overtimeSpent> { }

		[PXDBInt]
		[PXTimeList]
		[PXDefault(0, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXFormula(typeof(Switch<Case<Where<Selector<earningTypeID, EPEarningType.isOvertime>, Equal<True>>, timeSpent>, int0>))]
		[PXUIField(DisplayName = "Overtime", Enabled = false)]
		[PXFormula(null, typeof(SumCalc<EPTimeActivitiesSummary.totalOvertime>))]
		public virtual int? OvertimeSpent { get; set; }
		#endregion

		#region TimeBillable

		public abstract class timeBillable : PX.Data.BQL.BqlInt.Field<timeBillable> { }

		[PXDBInt]
		[PXTimeList]
		[PXDefault(0, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXFormula(typeof(
			Switch<Case<Where<isBillable, Equal<True>>, timeSpent,
				Case<Where<isBillable, Equal<False>>, int0>>,
				timeBillable>))]
		[PXUIField(DisplayName = "Billable Time", FieldClass = "BILLABLE")]
		[PXUIVerify(typeof(Where<isBillable,Equal<False>,
			Or<timeSpent, IsNull,
			Or<timeBillable, IsNull, 
				Or<timeSpent, GreaterEqual<timeBillable>>>>>), 
			PXErrorLevel.Error, Messages.BillableTimeCannotBeGreaterThanTimeSpent)]
		[PXUIVerify(typeof(Where<isBillable, NotEqual<True>, 
			Or<timeBillable, NotEqual<int0>>>), PXErrorLevel.Error, Messages.BillableTimeMustBeOtherThanZero,
			CheckOnInserted = false, CheckOnVerify = false)]
		[PXUnboundFormula(typeof(Switch<Case<Where<Selector<earningTypeID, EPEarningType.isOvertime>, Equal<False>>, timeBillable>, int0>), typeof(SumCalc<EPTimeActivitiesSummary.totalBillableTime>))]
		public virtual int? TimeBillable { get; set; }
		#endregion

		#region OvertimeBillable

		public abstract class overtimeBillable : PX.Data.BQL.BqlInt.Field<overtimeBillable> { }

		[PXDBInt]
		[PXTimeList]
		[PXDefault(0, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIVerify(typeof(Where<overtimeSpent, IsNull, 
			Or<overtimeBillable, IsNull, 
				Or<overtimeSpent, GreaterEqual<overtimeBillable>>>>), PXErrorLevel.Error, Messages.OvertimeBillableCannotBeGreaterThanOvertimeSpent)]
		[PXFormula(typeof(
			Switch<Case<Where<isBillable, Equal<True>, And<overtimeSpent, GreaterEqual<timeBillable>>>, timeBillable,
				Case<Where<isBillable, Equal<True>, And<overtimeSpent, GreaterEqual<Zero>>>, overtimeBillable,
					Case<Where<isBillable, Equal<False>>, int0>>>,
				overtimeBillable>))]
		[PXUIField(DisplayName = "Billable Overtime", FieldClass = "BILLABLE")]
		[PXFormula(null, typeof(SumCalc<EPTimeActivitiesSummary.totalBillableOvertime>))]
		public virtual int? OvertimeBillable { get; set; }
		#endregion

		#region Billed

		public abstract class billed : PX.Data.BQL.BqlBool.Field<billed> { }

		[PXDBBool]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Billed", FieldClass = "BILLABLE")]
		public virtual bool? Billed { get; set; }
		#endregion

		#region Released

		public abstract class released : PX.Data.BQL.BqlBool.Field<released> { }

		[PXDBBool]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Released", Enabled = false, Visible = false, FieldClass = "BILLABLE")]
		public virtual bool? Released { get; set; }
		#endregion

		#region IsCorrected

		public abstract class isCorrected : PX.Data.BQL.BqlBool.Field<isCorrected> { }

		/// <summary>
		/// If true this Activity has been corrected in the Timecard and is no longer valid. Please hide this activity in all lists displayed in the UI since there is another valid activity.
		/// The valid activity has a refence back to the corrected activity via OrigTaskID field. 
		/// </summary>
		[PXDBBool]
		[PXDefault(false)]
		public virtual bool? IsCorrected { get; set; }
		#endregion

		#region OrigNoteID

		public abstract class origNoteID : PX.Data.BQL.BqlGuid.Field<origNoteID> { }

		/// <summary>
		/// Use for correction. Stores the reference to the original activity.
		/// </summary>
		[PXDBGuid]
		public virtual Guid? OrigNoteID { get; set; }
		#endregion

		#region TranID

		public abstract class tranID : PX.Data.BQL.BqlLong.Field<tranID> { }

		[PXDBLong]
		public virtual long? TranID { get; set; }
		#endregion

		#region WeekID

		public abstract class weekID : PX.Data.BQL.BqlInt.Field<weekID> { }

		[PXDBInt]
		[PXUIField(DisplayName = "Time Card Week", Enabled = false)]
		[PXWeekSelector2]
		[PXFormula(typeof(Default<date>))]
		[EPActivityDefaultWeek(typeof(date))]
		public virtual int? WeekID { get; set; }
		#endregion

		#region LabourItemID

		public abstract class labourItemID : PX.Data.BQL.BqlInt.Field<labourItemID> { }

		[PMLaborItem(typeof(projectID), typeof(earningTypeID), typeof(Select<EPEmployee, Where<EPEmployee.defContactID, Equal<Current<ownerID>>>>))]
		[PXForeignReference(typeof(Field<labourItemID>.IsRelatedTo<InventoryItem.inventoryID>))]
		public virtual int? LabourItemID { get; set; }
		#endregion

		#region WorkCodeID

		public abstract class workCodeID : PX.Data.BQL.BqlString.Field<workCodeID> { }
		[PXForeignReference(typeof(FK.WorkCode))]
		[PMWorkCodeInTimeActivity(typeof(costCodeID), typeof(projectID), typeof(projectTaskID), typeof(labourItemID), typeof(ownerID))]
		public virtual string WorkCodeID { get; set; }
		#endregion

		#region OvertimeItemID

		public abstract class overtimeItemID : PX.Data.BQL.BqlInt.Field<overtimeItemID> { }

		[PXDBInt]
		[PXUIField(Visible = false)]
		public virtual int? OvertimeItemID { get; set; }
		#endregion

		#region JobID

		public abstract class jobID : PX.Data.BQL.BqlInt.Field<jobID> { }

		[PXDBInt]
		public virtual int? JobID { get; set; }
		#endregion

		#region ShiftID

		public abstract class shiftID : PX.Data.BQL.BqlInt.Field<shiftID> { }

		[PXDBInt]
		[PXUIField(DisplayName = "Shift Code", FieldClass = nameof(FeaturesSet.ShiftDifferential))]
		[TimeActivityShiftCodeSelector(typeof(ownerID), typeof(date))]
		[EPShiftCodeActiveRestrictor]
		public virtual int? ShiftID { get; set; }
		#endregion

		#region EmployeeRate

		public abstract class employeeRate : PX.Data.BQL.BqlDecimal.Field<employeeRate> { }

		/// <summary>
		/// Stores Employee's Hourly rate at the time the activity was released to PM
		/// </summary>
		[IN.PXDBPriceCost]
		[PXUIField(DisplayName = "Cost Rate", Enabled = false)]
		public virtual decimal? EmployeeRate { get; set; }
		#endregion

		#region SummaryLineNbr

		public abstract class summaryLineNbr : PX.Data.BQL.BqlInt.Field<summaryLineNbr> { }

		/// <summary>
		/// This is a adjusting activity for the summary line in the Timecard.
		/// </summary>
		[PXDBInt]
		public virtual int? SummaryLineNbr { get; set; }
		#endregion

		#region ARDocType

		public abstract class arDocType : PX.Data.BQL.BqlString.Field<arDocType> { }
		[AR.ARDocType.List()]
		[PXString(3, IsFixed = true)]
		[PXUIField(DisplayName = "Type", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		public virtual String ARDocType { get; set; }
		#endregion

		#region ARRefNbr

		public abstract class arRefNbr : PX.Data.BQL.BqlString.Field<arRefNbr> { }
		[PXString(15, IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Reference Nbr.", Visibility = PXUIVisibility.SelectorVisible)]
		[PXSelector(typeof(Search<AR.ARRegister.refNbr, Where<AR.ARRegister.docType, Equal<Current<arDocType>>>>), DescriptionField = typeof(AR.ARRegister.docType))]
		public virtual string ARRefNbr { get; set; }
		#endregion

		#region ReportedInTimeZoneID

		public abstract class reportedInTimeZoneID : PX.Data.BQL.BqlString.Field<reportedInTimeZoneID> { }

		[PXUIField(DisplayName = "Reported in Time Zone", Enabled = false, Visible = false)]
		[PXDBString(32)]
		[PXTimeZone]
		public virtual String ReportedInTimeZoneID { get; set; }
		#endregion


		#region CreatedByID

		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }

		[PXDBCreatedByID(DontOverrideValue = true)]
		[PXUIField(Enabled = false)]
		public virtual Guid? CreatedByID { get; set; }
		#endregion

		#region CreatedByScreenID

		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }

		[PXDBCreatedByScreenID]
		public virtual string CreatedByScreenID { get; set; }
		#endregion

		#region CreatedDateTime

		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }

		[PXUIField(DisplayName = "Created At", Enabled = false)]
		[PXDBCreatedDateTime]
		public virtual DateTime? CreatedDateTime { get; set; }
		#endregion

		#region LastModifiedByID

		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }

		[PXDBLastModifiedByID]
		public virtual Guid? LastModifiedByID { get; set; }
		#endregion

		#region LastModifiedByScreenID

		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }

		[PXDBLastModifiedByScreenID]
		public virtual string LastModifiedByScreenID { get; set; }
		#endregion

		#region LastModifiedDateTime

		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }

		[PXDBLastModifiedDateTime]
		public virtual DateTime? LastModifiedDateTime { get; set; }
		#endregion

		#region tstamp

		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }

		[PXDBTimestamp]
		public virtual byte[] tstamp { get; set; }
        #endregion



        #region NeedToBeDeleted

        public abstract class needToBeDeleted : PX.Data.BQL.BqlBool.Field<needToBeDeleted> { }

        [PXBool]
        [PXFormula(typeof(Switch<
				Case<Where<trackTime, NotEqual<True>,
					And<Where<projectID, IsNull, Or<projectID, Equal<NonProject>>>>>, True>,
			False>))]
		public bool? NeedToBeDeleted { get; set; }
		#endregion

		#region IsActivityExists

		public abstract class isActivityExists : PX.Data.BQL.BqlBool.Field<isActivityExists> { }

		[PXBool]
		[PXUIField(DisplayName = "Activity Exists", Enabled = false, Visible = false)]
		[PXFormula(typeof(Switch<
				Case<Where<refNoteID, NotEqual<noteID>>, True>,
			Null>))]
		public bool? IsActivityExists { get; set; }
		#endregion
	}