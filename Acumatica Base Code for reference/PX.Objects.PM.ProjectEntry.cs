using PX.Api;
using PX.Data;
using PX.Data.DependencyInjection;
using PX.LicensePolicy;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CA;
using PX.Objects.CM.Extensions;
using PX.Objects.Common;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.CT;
using PX.Objects.EP;
using PX.Objects.Extensions.MultiCurrency;
using PX.Objects.GL;
using PX.Objects.GL.FinPeriods;
using PX.Objects.IN;
using PX.SM;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Data.BQL.Fluent;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Data.BQL;
using PX.Common;

namespace PX.Objects.PM
{
	[Serializable]
    public class ProjectEntry : PXGraph<ProjectEntry, PMProject>, PXImportAttribute.IPXPrepareItems, IGraphWithInitialization
	{
		public class MultiCurrency : MultiCurrencyGraph<ProjectEntry, PMProject>
		{
			protected override string Module => GL.BatchModule.PM;

			protected override CurySourceMapping GetCurySourceMapping()
			{
				return new CurySourceMapping(typeof(Customer));
			}

			protected override DocumentMapping GetDocumentMapping()
			{
				return new DocumentMapping(typeof(PMProject))
				{
				};
			}

			protected override CurySource CurrentSourceSelect()
			{
				CurySource curySource = base.CurrentSourceSelect();
				if (curySource == null) return null;
				else
				{
					curySource.AllowOverrideCury = PXAccess.FeatureInstalled<FeaturesSet.projectMultiCurrency>();
					curySource.AllowOverrideRate = true;
					return curySource;
				}
			}

			protected override PXSelectBase[] GetChildren() => new PXSelectBase[]
			{
				Base.Project,
				Base.Tasks,
				Base.BalanceRecords,
				Base.RevenueBudget,
				Base.CostBudget,
				Base.dummyProforma,
				Base.dummyInvoice
			};

			protected override PXSelectBase[] GetTrackedExceptChildren() => new PXSelectBase[] { Base.TaskTotals };

			protected virtual void _(Events.FieldDefaulting<PMProject, PMProject.curyIDCopy> e)
			{
				if (e.Row == null) return;

				if (PXAccess.FeatureInstalled<FeaturesSet.projectMultiCurrency>())
				{
					e.NewValue = Base.Accessinfo.BaseCuryID ?? Base.Company.Current.BaseCuryID;
				}
				else
				{
					e.NewValue = e.Row.BaseCuryID;
				}
			}

			protected virtual void _(Events.FieldUpdated<PMProject, PMProject.curyIDCopy> e)
			{
				if (e.Row != null && e.ExternalCall)
				{
					e.Cache.SetDefaultExt<PMProject.billingCuryID>(e.Row);
					e.Cache.SetValueExt<PMProject.curyID>(e.Row, e.Row.CuryIDCopy);

					Base.ShowWaringOnProjectCurrecyIfExcahngeRateNotFound(e.Row);
				}
			}

			protected void _(Events.FieldVerifying<PMProject, PMProject.curyIDCopy> e)
			{
				ThrowIfCuryIDCannotBeChangedDueToExistingTransactions(e.Row?.CuryID, e.NewValue as string);
			}

			private void ThrowIfCuryIDCannotBeChangedDueToExistingTransactions(string currentCuryID, string newCuryIDValue)
			{
				if (currentCuryID == null) return;
				if (currentCuryID == newCuryIDValue) return;
				if (Base.ProjectHasTransactions())
					throw new PXSetPropertyException<PMProject.curyIDCopy>(Messages.ProjectCuryCannotBeChanged);
			}
		}

		#region Inner DACs
		[PXCacheName(Messages.SelectedTask)]
		[Serializable]
		[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		public partial class SelectedTask : PMTask
		{
			#region ProjectID
			public new abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID> { }

			[PXDBInt(IsKey = true)]
			public override Int32? ProjectID
			{
				get
				{
					return _ProjectID;
				}
				set
				{
					_ProjectID = value;
				}
			}
			#endregion
			#region TaskCD

			public new abstract class taskCD : PX.Data.BQL.BqlString.Field<taskCD> { }
			[PXDimension(ProjectTaskAttribute.DimensionName)]
			[PXDBString(30, IsUnicode = true, IsKey = true)]
			[PXUIField(DisplayName = "Task ID")]
			public override String TaskCD
			{
				get
				{
					return _TaskCD;
				}
				set
				{
					_TaskCD = value;
				}
			}
			#endregion

			public new abstract class taskID : PX.Data.BQL.BqlInt.Field<taskID> { }

			public new abstract class description : PX.Data.BQL.BqlString.Field<description> { }
		}

		#endregion

		#region DAC Attributes Override

		#region PMProject

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = "Currency Rate for Budget", IsReadOnly = true, FieldClass = nameof(FeaturesSet.ProjectMultiCurrency))]
		protected virtual void _(Events.CacheAttached<PMProject.curyID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDimensionSelector(ProjectAttribute.DimensionNameTemplate,
				typeof(Search2<PMProjectSearch.contractID,
						LeftJoin<ContractBillingSchedule, On<ContractBillingSchedule.contractID, Equal<PMProjectSearch.contractID>>>,
							Where<PMProjectSearch.baseType, Equal<CT.CTPRType.projectTemplate>, And<PMProjectSearch.isActive, Equal<True>>>>),
				typeof(PMProjectSearch.contractCD),
				typeof(PMProjectSearch.contractCD),
				typeof(PMProjectSearch.description),
				typeof(PMProjectSearch.budgetLevel),
				typeof(PMProjectSearch.billingID),
				typeof(ContractBillingSchedule.type),
				typeof(PMProjectSearch.ownerID),
				DescriptionField = typeof(PMProjectSearch.description))]
		protected virtual void _(Events.CacheAttached<PMProject.templateID> e) { }
		#endregion

		#region PMTask

		[PXDBInt(IsKey = true)]
		[PXParent(typeof(Select<PMProject, Where<PMProject.contractID, Equal<Current<PMTask.projectID>>>>))]
		[PXDBDefault(typeof(PMProject.contractID))]
		protected virtual void _(Events.CacheAttached<PMTask.projectID> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[Account(DisplayName = "Default Sales Account", Visible = false)]
		protected virtual void _(Events.CacheAttached<PMTask.defaultSalesAccountID> e)
		{
		}

		[PXDefault(typeof(PMProject.defaultSalesSubID), PersistingCheck = PXPersistingCheck.Nothing)]
		[SubAccount(DisplayName = "Default Sales Subaccount", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description), Visible = false)]
		protected virtual void _(Events.CacheAttached<PMTask.defaultSalesSubID> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[Account(DisplayName = "Default Cost Account", Visible = false)]
		protected virtual void _(Events.CacheAttached<PMTask.defaultExpenseAccountID> e)
		{
		}

		[PXDefault(typeof(PMProject.defaultExpenseSubID), PersistingCheck = PXPersistingCheck.Nothing)]
		[SubAccount(DisplayName = "Default Cost Subaccount", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description), Visible = false)]
		protected virtual void _(Events.CacheAttached<PMTask.defaultExpenseSubID> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = "Planned Start Date", Visible = false)]
		protected virtual void _(Events.CacheAttached<PMTask.plannedStartDate> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = "Planned End Date", Visible = false)]
		protected virtual void _(Events.CacheAttached<PMTask.plannedEndDate> e)
		{
		}

		[PXDBBool()]
        [PXDefault(false)]
        [PXUIField(DisplayName = "Bill Separately", Visible = false)]
        protected virtual void _(Events.CacheAttached<PMTask.billSeparately> e)
        {
        }

        #endregion

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDefault(BAccountType.CustomerType)]
        protected virtual void _(Events.CacheAttached<BAccountR.type> e)
        {
        }

        [PXDBCurrency(typeof(PMProject.curyInfoID), typeof(PMTaskTotal.income))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Actual Income", Enabled = false)]
		protected virtual void _(Events.CacheAttached<PMTaskTotal.curyIncome> e) { }

		[PXBaseCury]
		protected virtual void _(Events.CacheAttached<PMTaskTotal.income> e) { }

		[PXDBCurrency(typeof(PMProject.curyInfoID), typeof(PMTaskTotal.expense))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Actual Expenses", Enabled = false)]
		protected virtual void _(Events.CacheAttached<PMTaskTotal.curyExpense> e) { }

		[PXBaseCury]
		protected virtual void _(Events.CacheAttached<PMTaskTotal.expense> e) { }

		[PXBaseCury]
		protected virtual void _(Events.CacheAttached<PMTaskTotal.asset> e) { }

		[PXBaseCury]
		protected virtual void _(Events.CacheAttached<PMTaskTotal.liability> e) { }

		#region EPEquipmentRate

		[PXDBInt(IsKey = true)]
		[PXDefault]
		[PXUIField(DisplayName = "Equipment ID")]
		[PXSelector(typeof(EPEquipment.equipmentID), DescriptionField = typeof(EPEquipment.description), SubstituteKey = typeof(EPEquipment.equipmentCD))]
		protected virtual void _(Events.CacheAttached<EPEquipmentRate.equipmentID> e)
		{
		}


		[PXParent(typeof(Select<PMProject, Where<PMProject.contractID, Equal<Current<EPEquipmentRate.projectID>>>>))]
		[PXDBDefault(typeof(PMProject.contractID))]
		[PXDBInt(IsKey = true)]
		protected virtual void _(Events.CacheAttached<EPEquipmentRate.projectID> e)
		{
		}

				
		#endregion

		[PXDBString(1, IsFixed = true)]
		[BillingType.ListForProject()]
		[PXUIField(DisplayName = "Billing Period")]
		protected virtual void _(Events.CacheAttached<ContractBillingSchedule.type> e)
		{
		}

		#region ARInvoice

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = "AR Doc. Status")]
		protected virtual void _(Events.CacheAttached<ARInvoice.status> e) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = "AR Doc. Description")]
		protected virtual void _(Events.CacheAttached<ARInvoice.docDesc> e) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = "Original Document", Enabled = false)]
		protected virtual void _(Events.CacheAttached<ARInvoice.origRefNbr> e) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = "AR Doc. Total Amount", FieldClass = nameof(FeaturesSet.Retainage))]
		[PXFormula(typeof(Add<ARRegister.curyOrigDocAmt, ARRegister.curyRetainageTotal>))]
		protected virtual void _(Events.CacheAttached<ARInvoice.curyOrigDocAmtWithRetainageTotal> e) { }

		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXUIField(DisplayName = "AR Doc. Orig. Amount")]
		[PXDBCurrency(typeof(ARInvoice.curyInfoID), typeof(ARInvoice.origDocAmt))]//Should be removed on switching PM to new CM
		protected virtual void _(Events.CacheAttached<ARInvoice.curyOrigDocAmt> e) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = "AR Doc. Date")]
		protected virtual void _(Events.CacheAttached<ARInvoice.docDate> e) { }

		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXUIField(DisplayName = "Open AR Balance")]
		[PXDBCurrency(typeof(ARRegister.curyInfoID), typeof(ARRegister.docBal), BaseCalc = false)]//Should be removed on switching PM to new CM
		protected virtual void _(Events.CacheAttached<ARInvoice.curyDocBal> e) { }

		#endregion

		#region Proforma

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = "Invoice Total")]
		protected virtual void _(Events.CacheAttached<PMProforma.docTotal> e) { }

		[PXDBDate]
		[PXUIField(DisplayName = "Pro Forma Date", Visibility = PXUIVisibility.SelectorVisible)]
		protected virtual void _(Events.CacheAttached<PMProforma.invoiceDate> e) { }

		#endregion

		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXBool]
		[PXDefault(false)]
		protected virtual void _(Events.CacheAttached<PMCostCode.isProjectOverride> e)
		{
		}

		#region EPEmployeeContract
		[PXDBInt(IsKey = true)]
		[PXDefault]
		[PXUIField(DisplayName = "Employee ID", Visibility = PXUIVisibility.Visible)]
		[EP.PXEPEmployeeSelector()]
		protected virtual void _(Events.CacheAttached<EPEmployeeContract.employeeID> e)
		{
		}

		[PXDBInt(IsKey = true)]
		[PXDBDefault(typeof(PMProject.contractID))]
		[PXParent(typeof(Select<PMProject, Where<PMProject.contractID, Equal<Current<EPEmployeeContract.contractID>>>>))]
		protected virtual void _(Events.CacheAttached<EPEmployeeContract.contractID> e)
		{
		}

		[PXDBIdentity]
		protected virtual void _(Events.CacheAttached<EPContractRate.recordID> e)
		{
		}

		[PXDBInt(IsKey = true)]
		[PXDBDefault(typeof(PMProject.contractID))]
		protected virtual void _(Events.CacheAttached<EPContractRate.contractID> e)
		{
		}

		[PXDBInt(IsKey = true)]
		[PXParent(typeof(Select<EPEmployeeContract, Where<EPEmployeeContract.employeeID, Equal<Current<EPContractRate.employeeID>>, And<EPEmployeeContract.contractID, Equal<Current<EPContractRate.contractID>>>>>))]
		[PXDefault(typeof(EPEmployeeContract.employeeID))]
		protected virtual void _(Events.CacheAttached<EPContractRate.employeeID> e)
		{
		}

		[PXDBString(EPEarningType.typeCD.Length, IsKey = true, IsUnicode = true, InputMask = EPEarningType.typeCD.InputMask)]
		[PXDefault]
		[PXRestrictor(typeof(Where<EP.EPEarningType.isActive, Equal<True>>), EP.Messages.EarningTypeInactive, typeof(EP.EPEarningType.typeCD))]
		[PXSelector(typeof(EP.EPEarningType.typeCD))]
		[PXUIField(DisplayName = "Earning Type")]
		protected virtual void _(Events.CacheAttached<EPContractRate.earningType> e)
		{
		}		

		#endregion

		#region NotificationSource
		[PXDBGuid(IsKey = true)]
		[PXSelector(typeof(Search<NotificationSetup.setupID,
			Where<NotificationSetup.sourceCD, Equal<PMNotificationSource.project>>>),
			DescriptionField = typeof(NotificationSetup.notificationCD),
			SelectorMode = PXSelectorMode.DisplayModeText | PXSelectorMode.NoAutocomplete)]
		[PXUIField(DisplayName = "Mailing ID")]
		protected virtual void _(Events.CacheAttached<NotificationSource.setupID> e)
		{
		}
		[PXDBString(10, IsUnicode = true)]
		protected virtual void _(Events.CacheAttached<NotificationSource.classID> e)
		{
		}
		[GL.Branch(useDefaulting: false, IsDetail = false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXCheckUnique(typeof(NotificationSource.setupID), IgnoreNulls = false,
			Where = typeof(Where<NotificationSource.refNoteID, Equal<Current<NotificationSource.refNoteID>>>))]
		protected virtual void _(Events.CacheAttached<NotificationSource.nBranchID> e)
		{

		}
		[PXDBString(8, InputMask = "CC.CC.CC.CC")]
		[PXUIField(DisplayName = "Report")]
		[PXDefault(typeof(Search<NotificationSetup.reportID,
			Where<NotificationSetup.setupID, Equal<Current<NotificationSource.setupID>>>>),
			PersistingCheck = PXPersistingCheck.Nothing)]
		[PXSelector(typeof(Search<SiteMap.screenID,
			Where<SiteMap.url, Like<Common.urlReports>,
				And<Where<SiteMap.screenID, Like<PXModule.pm_>>>>,
			OrderBy<Asc<SiteMap.screenID>>>), typeof(SiteMap.screenID), typeof(SiteMap.title),
			Headers = new string[] { CA.Messages.ReportID, CA.Messages.ReportName },
			DescriptionField = typeof(SiteMap.title))]
		[PXFormula(typeof(Default<NotificationSource.setupID>))]
		protected virtual void _(Events.CacheAttached<NotificationSource.reportID> e)
		{
		}
		#endregion

		#region NotificationRecipient
		[PXDBInt]
		[PXDBDefault(typeof(NotificationSource.sourceID))]
		protected virtual void _(Events.CacheAttached<NotificationRecipient.sourceID> e)
		{
		}
		[PXDBString(10)]
		[PXDefault]
		[NotificationContactType.ProjectList]
		[PXUIField(DisplayName = "Contact Type")]
		[PXCheckUnique(typeof(NotificationRecipient.contactID),
			Where = typeof(Where<NotificationRecipient.sourceID, Equal<Current<NotificationRecipient.sourceID>>,
			And<NotificationRecipient.refNoteID, Equal<Current<PMProject.noteID>>>>))]
		protected virtual void _(Events.CacheAttached<NotificationRecipient.contactType> e)
		{
		}
		[PXDBInt]
		[PXUIField(DisplayName = "Contact ID")]
		[PXNotificationContactSelector(typeof(NotificationRecipient.contactType),
			typeof(Search2<Contact.contactID,
				LeftJoin<BAccountR, On<BAccountR.bAccountID, Equal<Contact.bAccountID>>,
				LeftJoin<EPEmployee,
					  On<EPEmployee.parentBAccountID, Equal<Contact.bAccountID>,
					  And<EPEmployee.defContactID, Equal<Contact.contactID>>>>>,
				Where2<
						Where<Current<NotificationRecipient.contactType>, Equal<NotificationContactType.employee>,
			  And<EPEmployee.acctCD, IsNotNull>>,
					 Or<Where<Current<NotificationRecipient.contactType>, Equal<NotificationContactType.contact>,
								And<BAccountR.bAccountID, Equal<Current<PMProject.customerID>>,
								And<Contact.contactType, Equal<ContactTypesAttribute.person>>>>>>>)
			, DirtyRead = true)]
		protected virtual void _(Events.CacheAttached<NotificationRecipient.contactID> e)
		{
		}
		[PXDBString(10, IsUnicode = true)]
		protected virtual void _(Events.CacheAttached<NotificationRecipient.classID> e)
		{
		}
		[PXString()]
		[PXUIField(DisplayName = "Email", Enabled = false)]
		protected virtual void _(Events.CacheAttached<NotificationRecipient.email> e)
		{
		}
		#endregion

		#region EPApproval Cache Attached - Approvals Fields
		[PXDefault(typeof(PMProject.startDate), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void _(Events.CacheAttached<EPApproval.docDate> e)
		{
		}

		[PXDefault(typeof(PMProject.customerID), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void _(Events.CacheAttached<EPApproval.bAccountID> e)
		{
		}

		[PXDefault(typeof(PMProject.ownerID), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void _(Events.CacheAttached<EPApproval.documentOwnerID> e)
		{
		}

		[PXDefault(typeof(PMProject.description), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void _(Events.CacheAttached<EPApproval.descr> e)
		{
		}
		#endregion

		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[EPUnboundStartDate]
		[PXUIField(DisplayName = "Start Date")]
		[PXFormula(typeof(IsNull<Current<CRActivity.startDate>, Current<PMCRActivity.date>>))]
		protected virtual void _(Events.CacheAttached<PMCRActivity.startDate> e)
		{ }

		[PXDBGuid(IsKey = true)]
		protected virtual void _(Events.CacheAttached<CROpportunityProducts.quoteID> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = "Curr. Rate Type ID", IsReadOnly = true)]
		protected virtual void _(Events.CacheAttached<CurrencyInfo.curyRateTypeID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXDBDefault(typeof(PMProject.contractID))]
		[PXDBInt(IsKey = true)]
		protected virtual void _(Events.CacheAttached<PMRetainageStep.projectID> e){ }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDimensionSelector("PROTASK",
            typeof(Search<PMTask.taskID, Where<PMTask.projectID, Equal<Current<PMProject.contractID>>, 
					And<PMTask.type, NotEqual<CN.ProjectAccounting.PM.Descriptor.ProjectTaskType.cost>>>>),
                typeof(PMTask.taskCD),
                typeof(PMTask.description),
                typeof(PMTask.status),
                typeof(PMTask.taskCD),
                typeof(PMTask.type))]
        protected virtual void _(Events.CacheAttached<RevenueBudgetFilter.projectTaskID> e) { }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDimensionSelector("PROTASK",
			typeof(Search<PMTask.taskID, Where<PMTask.projectID, Equal<Current<PMProject.contractID>>, 
					And<PMTask.type, NotEqual<CN.ProjectAccounting.PM.Descriptor.ProjectTaskType.revenue>>>>),
				typeof(PMTask.taskCD),
                typeof(PMTask.description),
                typeof(PMTask.status),
                typeof(PMTask.taskCD),
                typeof(PMTask.type))]
        protected virtual void _(Events.CacheAttached<CostBudgetFilter.projectTaskID> e) { }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXParent(typeof(Select<PMTask, Where<PMTask.taskID, Equal<Current<PMForecastDetail.projectTaskID>>>>))]
		protected virtual void _(Events.CacheAttached<PMForecastDetail.projectTaskID> e) { }


		[PXDBLong]
		[CurrencyInfo(typeof(PMProject.curyInfoID))]
		[PXMergeAttributes(Method = MergeMethod.Replace)]
		protected virtual void PMRevenueBudget_CuryInfoID_CacheAttached(PXCache sender)
		{
		}

		[PXDBLong]
		[CurrencyInfo(typeof(PMProject.curyInfoID))]
		[PXMergeAttributes(Method = MergeMethod.Replace)]
		protected virtual void PMCostBudget_CuryInfoID_CacheAttached(PXCache sender)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDefault(typeof(Coalesce<SearchFor<Address.countryID>.In<SelectFrom<Address>
			.InnerJoin<Customer>.On<Customer.defBillAddressID.IsEqual<Address.addressID>>
				.Where<Customer.bAccountID.IsEqual<PMProject.customerID.FromCurrent>>>,
			SearchFor<CustomerClass.countryID>.In<SelectFrom<CustomerClass>
				.Where<CustomerClass.customerClassID.IsEqual<CustomerClass.customerClassID.FromCurrent>>>,
			SearchFor<GL.Branch.countryID>.In<SelectFrom<GL.Branch>
				.Where<GL.Branch.branchID.IsEqual<AccessInfo.branchID.FromCurrent>>>>))]
		protected virtual void _(Events.CacheAttached<PMSiteAddress.countryID> e) { }

		#endregion

		[InjectDependency]
		public IUnitRateService RateService { get; set; }

		#region Views/Selects/Delegates

		[PXViewName(Messages.Project)]
		public PXSelect<PMProject, Where<PMProject.baseType, Equal<CTPRType.project>,
			And<PMProject.nonProject, Equal<False>,
			And<Match<Current<AccessInfo.userName>>>>>> Project;

		[PXCopyPasteHiddenFields(typeof(PMProject.lastChangeOrderNumber))]
		public PXSelect<PMProject, Where<PMProject.contractID, Equal<Current<PMProject.contractID>>>> ProjectProperties;

		public PXSetup<Customer>.Where<Customer.bAccountID.IsEqual<PMProject.customerID.AsOptional>> customer;

		[PXCopyPasteHiddenView]
		[PXHidden]
		public PXSelect<BAccountR> dummyAccountR;

		[PXCopyPasteHiddenView]
		[PXHidden]
		public PXSelect<Vendor> dummyVendor;

		[PXCopyPasteHiddenView]
		public PXSelect<PX.Objects.CR.Standalone.EPEmployee> approver;

		[PXCopyPasteHiddenFields(typeof(ContractBillingSchedule.lastDate), typeof(ContractBillingSchedule.nextDate))]
		public PXSelect<CT.ContractBillingSchedule, Where<CT.ContractBillingSchedule.contractID, Equal<Current<PMProject.contractID>>>> Billing;

		[PXImport(typeof(PMProject))]
		[PXFilterable]
		[PXCopyPasteHiddenFields(typeof(PMTask.completedPercent), typeof(PMTask.endDate))]
		public PXSelect<PMTask, Where<PMTask.projectID, Equal<Current<PMProject.contractID>>>> Tasks;

		

		public PXSelectJoin<EPEmployeeContract,
			InnerJoin<CR.Standalone.EPEmployee, On<CR.Standalone.EPEmployee.bAccountID, Equal<EPEmployeeContract.employeeID>>>,
			Where<EPEmployeeContract.contractID, Equal<Current<PMProject.contractID>>>> EmployeeContract;
		public PXSelectJoin<EPContractRate
				, LeftJoin<IN.InventoryItem, On<IN.InventoryItem.inventoryID, Equal<EPContractRate.labourItemID>>
					, LeftJoin<EPEarningType, On<EPEarningType.typeCD, Equal<EPContractRate.earningType>>>
					>
				, Where<EPContractRate.employeeID, Equal<Optional<EPEmployeeContract.employeeID>>, And<EPContractRate.contractID, Equal<Optional<PMProject.contractID>>>>
				, OrderBy<Asc<EPContractRate.contractID>>
				> ContractRates;
		[PXFilterable]
		public PXSelectJoin<EPEquipmentRate, InnerJoin<EPEquipment, On<EPEquipmentRate.equipmentID, Equal<EPEquipment.equipmentID>>>, Where<EPEquipmentRate.projectID, Equal<Current<PMProject.contractID>>>> EquipmentRates;
		public PXSelect<PMAccountTask, Where<PMAccountTask.projectID, Equal<Current<PMProject.contractID>>>> Accounts;

		public EPDependNoteList<NotificationSource, NotificationSource.refNoteID, PMProject> NotificationSources;
		public PXSelect<NotificationRecipient, Where<NotificationRecipient.sourceID, Equal<Optional<NotificationSource.sourceID>>>> NotificationRecipients;

		public PXSelect<PMProjectRevenueTotal, Where<PMProjectRevenueTotal.projectID, Equal<Current<PMProject.contractID>>>> ProjectRevenueTotals;
		public PXSelect<PMRetainageStep, Where<PMRetainageStep.projectID, Equal<Current<PMProject.contractID>>>, OrderBy<Asc<PMRetainageStep.thresholdPct>>> RetainageSteps;
		
		public ChangeProjectID ChangeID;
		public PXSelectJoin<INCostCenter, 
			InnerJoin<PMTask, On <PMTask.projectID, Equal<INCostCenter.projectID>, And<PMTask.taskID, Equal<INCostCenter.taskID>>>,
			InnerJoin<IN.INLocation, On<IN.INLocation.locationID, Equal<INCostCenter.locationID>>>>,
			Where<INCostCenter.costLayerType, Equal<CostLayerType.project>,
				And<INCostCenter.projectID, Equal<Current<PMProject.contractID>>>>> CostCenters;

		[PXCopyPasteHiddenView]
		[PXHidden]
		public PXSelect<ARInvoice, Where<ARInvoice.projectID, Equal<Current<PMProject.contractID>>>> dummyInvoice;//for cache attached to rename fields for joinned table in Invoices view.

		[PXCopyPasteHiddenView]
		[PXHidden]
		public PXSelect<PMProforma, Where<PMProforma.projectID,Equal<Current<PMProject.contractID>>>> dummyProforma;//for cache attached to rename fields for joinned table in Invoices view.
				
		[PXCopyPasteHiddenView]
		[PXHidden]
		public PXSelect<PMCostCode> dummyCostCode;

		[PXFilterable]
		[PXCopyPasteHiddenView]
		public PXSelectJoin<PMBillingRecord,
			LeftJoin<PMProforma, On<PMProforma.refNbr, Equal<PMBillingRecord.proformaRefNbr>, And<PMProforma.corrected, Equal<False>>>,
			LeftJoin<ARInvoice, On<ARInvoice.docType, Equal<PMBillingRecord.aRDocType>, And<ARInvoice.refNbr, Equal<PMBillingRecord.aRRefNbr>>>>>,
			Where<PMBillingRecord.projectID, Equal<Current<PMProject.contractID>>>,
			OrderBy<Asc<PMBillingRecord.sortOrder, Asc<PMBillingRecord.proformaRefNbr>>>> Invoices;

		public virtual IEnumerable invoices()
		{
			var manualInvoiceSelect = new PXSelectReadonly2<ARInvoice, 
				LeftJoin<PMBillingRecord, On<ARInvoice.docType, Equal<PMBillingRecord.aRDocType>, And<ARInvoice.refNbr, Equal<PMBillingRecord.aRRefNbr>>>>,
				Where<PMBillingRecord.recordID, IsNull, And<ARInvoice.projectID, Equal<Current<PMProject.contractID>>>>> (this);

			var manualResult = manualInvoiceSelect.Select();

			if (manualResult.Count == 0)
			{
				var view = new PXView(this, false, Invoices.View.BqlSelect);
				var startRow = PXView.StartRow;
				int totalRows = 0;
				
				var list = view.Select(PXView.Currents, PXView.Parameters, PXView.Searches, PXView.SortColumns, PXView.Descendings, PXView.Filters,
									   ref startRow, PXView.MaximumRows, ref totalRows);

				PXView.StartRow = 0;

				return list;
			}
			else
			{
				HashSet<string> added = new HashSet<string>();

				var autoInvoicesSelect = new PXSelectReadonly2<PMBillingRecord,
					LeftJoin<PMProforma, On<PMProforma.refNbr, Equal<PMBillingRecord.proformaRefNbr>, And<PMProforma.corrected, Equal<False>>>,
					LeftJoin<ARInvoice, On<ARInvoice.docType, Equal<PMBillingRecord.aRDocType>, And<ARInvoice.refNbr, Equal<PMBillingRecord.aRRefNbr>>>>>,
					Where<PMBillingRecord.projectID, Equal<Current<PMProject.contractID>>>>(this);

				PXResultset<PMBillingRecord> rs = autoInvoicesSelect.Select();

				List<PXResult<PMBillingRecord, PMProforma, ARInvoice>> list = new List<PXResult<PMBillingRecord, PMProforma, ARInvoice>>(rs.Count + manualResult.Count);
				foreach(PXResult<PMBillingRecord, PMProforma, ARInvoice> record in rs)
				{
					ARInvoice invoice = record;
					if (!string.IsNullOrEmpty(invoice.RefNbr))
					{
						added.Add(string.Format("{0}.{1}", invoice.DocType, invoice.RefNbr));
					}

					list.Add(record);
				}

				int cx = -1;
				foreach (PXResult<ARInvoice, PMBillingRecord> record in manualResult)
				{
					ARInvoice ar = (ARInvoice)record;
					PMBillingRecord br = (PMBillingRecord)record;

					if (added.Contains(string.Format("{0}.{1}", ar.DocType, ar.RefNbr)))
						continue;

					if (string.IsNullOrEmpty(br.ARDocType))
					{
						br.ARDocType = ar.DocType;
						br.ARRefNbr = ar.RefNbr;
						br.RecordID = cx--;
						br.BillingTag = "P";
						br.ProjectID = ar.ProjectID;
					}
										
					list.Add(new PXResult<PMBillingRecord, PMProforma, ARInvoice>(br, new PMProforma(), ar));
				}

					

				int order = 0;
				foreach (var item in list)
				{
					order++;
					var br = ((PMBillingRecord)item);
					br.SortOrder = order;

					br = (PMBillingRecord)Invoices.Cache.Insert(br);
					Invoices.Cache.SetStatus(br, PXEntryStatus.Held);
				}
				return list;
			}
		}

		public virtual int CompareBillingRecords(PXResult<PMBillingRecord, PMProforma, ARInvoice> x, PXResult<PMBillingRecord, PMProforma, ARInvoice> y)
		{
			
			DateTime? dateA = ((ARInvoice)x).DocDate ?? ((PMProforma)x).InvoiceDate;
			DateTime? dateB = ((ARInvoice)y).DocDate ?? ((PMProforma)y).InvoiceDate;

			if(dateA == null && dateB == null)
			{
				return 0;
			}

			if (dateA == null)
			{
				return -1;
			}

			if (dateB == null)
			{
				return 1;
			}

			if (dateA == dateB)
			{
				int billingIdA = (((PMBillingRecord)x).RecordID == null || ((PMBillingRecord)x).RecordID < 0) ? int.MaxValue : ((PMBillingRecord)x).RecordID.Value;
				int billingIdB = (((PMBillingRecord)y).RecordID == null || ((PMBillingRecord)y).RecordID < 0) ? int.MaxValue : ((PMBillingRecord)y).RecordID.Value;

				if (billingIdA == billingIdB)
				{
					if (billingIdA == int.MaxValue )
					{
						return ((ARInvoice)x).RefNbr.CompareTo(((ARInvoice)y).RefNbr);
					}
					else
					{
						return 0;
					}
				}
				else
				{
					return billingIdA.CompareTo(billingIdB);
				}
			}
			else
			{
				return dateA.Value.CompareTo(dateB.Value);
			}

		}
		

		public PXFilter<TemplateSettingsFilter> TemplateSettings;

		[PXHidden]
		public PXSelect<PMRecurringItem, Where<PMRecurringItem.projectID, Equal<Current<PMTask.projectID>>>> BillingItems;

		public PXSetup<PMSetup> Setup;
		public PXSetup<Company> Company;

		public PXSelectGroupBy<PMTaskTotal, Where<PMTaskTotal.projectID, Equal<Current<PMProject.contractID>>>,
			Aggregate<Sum<PMTaskTotal.asset, Sum<PMTaskTotal.liability, Sum<PMTaskTotal.income, Sum<PMTaskTotal.expense,
			Sum<PMTaskTotal.curyAsset, Sum<PMTaskTotal.curyLiability, Sum<PMTaskTotal.curyIncome, Sum<PMTaskTotal.curyExpense>>>>>>>>>> TaskTotals;

		[PXViewName(Messages.ProjectAnswers)]
		public CRAttributeList<PMProject> Answers;

		[PXHidden]//Used for deep copy of Tasks on Copy().
		public ProjectTaskAttributeList TaskAnswers;

		[PXViewName(Messages.PMAddress)]
		public PXSelect<PMAddress, Where<PMAddress.addressID, Equal<Current<PMProject.billAddressID>>>> Billing_Address;
		[PXViewName(Messages.PMContact)]
		public PXSelect<PMContact, Where<PMContact.contactID, Equal<Current<PMProject.billContactID>>>> Billing_Contact;
		[PXViewName(Messages.SiteAddress)]
		public PXSelect<PMSiteAddress, Where<PMSiteAddress.addressID, Equal<Current<PMProject.siteAddressID>>>> Site_Address;

		[PXCopyPasteHiddenView]
		[PXFilterable]
		[PXViewName(Messages.Activities)]
		[CRReference(typeof(Select<Customer, Where<Customer.bAccountID, Equal<Current<PMProject.customerID>>>>))]
		public ProjectActivities Activities;

		public PXSelect<CurrencyInfo, Where<CurrencyInfo.curyInfoID, Equal<Current<PMProject.curyInfoID>>>> CuryInfo;

		public CM.CMSetupSelect CMSetup;

		public PXFilter<CostBudgetFilter> CostFilter;

		[PXCopyPasteHiddenFields(typeof(PMCostBudget.revisedQty), typeof(PMCostBudget.curyRevisedAmount), typeof(PMCostBudget.curyCostToComplete), typeof(PMCostBudget.percentCompleted), typeof(PMCostBudget.curyCostAtCompletion), typeof(PMCostBudget.completedPct))]
		[PXImport(typeof(PMProject))]
		[PXDependToCache(typeof(PMProject), typeof(CostBudgetFilter))]
        [PXFilterable]
		public PXSelect<PMCostBudget, Where<PMCostBudget.projectID, Equal<Current<PMProject.contractID>>, And<PMCostBudget.type, Equal<GL.AccountType.expense>>>> CostBudget;
	    
        public IEnumerable costBudget()
		{
			var selectCostBudget = new PXSelect<PMCostBudget, Where<PMCostBudget.projectID, Equal<Current<PMProject.contractID>>, And<PMCostBudget.type, Equal<GL.AccountType.expense>,
			  And<Where<Current<CostBudgetFilter.projectTaskID>, IsNull, Or<Current<CostBudgetFilter.projectTaskID>, Equal<PMCostBudget.projectTaskID>>>>>>,
			  OrderBy<Asc<PMCostBudget.projectID, Asc<PMCostBudget.projectTaskID, Asc<PMCostBudget.inventoryID, Asc<PMCostBudget.costCodeID, Asc<PMCostBudget.accountGroupID>>>>>>>(this);

			PXDelegateResult delResult = new PXDelegateResult();
			delResult.Capacity = 202;
			delResult.IsResultFiltered = false;
			delResult.IsResultSorted = true;
			delResult.IsResultTruncated = false;

			if (IsCostGroupByTask() && !IsCopyPaste)
			{
				var list = new List<PMCostBudget>(selectCostBudget.Select().RowCast<PMCostBudget>());

				delResult.AddRange(AggregateBudget<PMCostBudget>(list));
			}
			else
			{
                var view = new PXView(this, false, selectCostBudget.View.BqlSelect);
                var startRow = PXView.StartRow;
                int totalRows = 0;

                var resultset = view.Select(PXView.Currents, PXView.Parameters, PXView.Searches, PXView.SortColumns, PXView.Descendings, PXView.Filters, ref startRow, PXView.MaximumRows, ref totalRows);
                PXView.StartRow = 0;

                delResult.AddRange(resultset.RowCast<PMCostBudget>());
			}
			return delResult;
		}
			
		public PXFilter<RevenueBudgetFilter> RevenueFilter;

		[PXCopyPasteHiddenFields(typeof(PMRevenueBudget.completedPct), typeof(PMRevenueBudget.revisedQty), typeof(PMRevenueBudget.curyRevisedAmount), typeof(PMRevenueBudget.curyAmountToInvoice))]
		[PXImport(typeof(PMProject))]
		[PXFilterable]
		[PXDependToCache(typeof(PMProject), typeof(RevenueBudgetFilter))]
		public PXSelect<PMRevenueBudget, Where<PMRevenueBudget.projectID, Equal<Current<PMProject.contractID>>, And<PMRevenueBudget.type, Equal<GL.AccountType.income>>>> RevenueBudget;

	    
        public IEnumerable revenueBudget()
		{
			var selectRevenueBudget = new PXSelect<PMRevenueBudget, Where<PMRevenueBudget.projectID, Equal<Current<PMProject.contractID>>, And<PMRevenueBudget.type, Equal<GL.AccountType.income>,
			  And<Where<Current<RevenueBudgetFilter.projectTaskID>, IsNull, Or<Current<RevenueBudgetFilter.projectTaskID>, Equal<PMRevenueBudget.projectTaskID>>>>>>,
			  OrderBy<Asc<PMRevenueBudget.projectID, Asc<PMRevenueBudget.projectTaskID, Asc<PMRevenueBudget.inventoryID, Asc<PMRevenueBudget.costCodeID, Asc<PMRevenueBudget.accountGroupID>>>>>>>(this);

			PXDelegateResult delResult = new PXDelegateResult();
			delResult.Capacity = 202;
			delResult.IsResultFiltered = false;
			delResult.IsResultSorted = true;
			delResult.IsResultTruncated = false;

			if (IsRevenueGroupByTask() && !IsCopyPaste)
			{
				var list = new List<PMRevenueBudget>(selectRevenueBudget.Select().RowCast<PMRevenueBudget>());
				delResult.AddRange(AggregateBudget<PMRevenueBudget>(list));
			}
			else
			{
                var view = new PXView(this, false, selectRevenueBudget.View.BqlSelect);
                var startRow = PXView.StartRow;
                int totalRows = 0;

                var resultset = view.Select(PXView.Currents, PXView.Parameters, PXView.Searches, PXView.SortColumns, PXView.Descendings, PXView.Filters, ref startRow, PXView.MaximumRows, ref totalRows);
                PXView.StartRow = 0;

				delResult.AddRange(resultset.RowCast<PMRevenueBudget>());
			}

			return delResult;
		}

		
		[PXImport(typeof(PMProject))]
		[PXFilterable]
		public PXSelect<PMOtherBudget, Where<PMOtherBudget.projectID, Equal<Current<PMProject.contractID>>, And<PMOtherBudget.type, NotEqual<GL.AccountType.income>, And<PMOtherBudget.type, NotEqual<GL.AccountType.expense>>>>> OtherBudget;

		[PXHidden]
		public PXSelect<PMForecastHistoryAccum> ForecastHistory;

		[PXCopyPasteHiddenView]
		[PXVirtualDAC]
		public PXSelect<PMProjectBalanceRecord, Where<PMProjectBalanceRecord.recordID, IsNotNull>,
			OrderBy<Asc<PMProjectBalanceRecord.sortOrder>>> BalanceRecords;

		public IEnumerable balanceRecords()
		{
			List<PMProjectBalanceRecord> asset = new List<PMProjectBalanceRecord>();
			List<PMProjectBalanceRecord> liability = new List<PMProjectBalanceRecord>();
			List<PMProjectBalanceRecord> income = new List<PMProjectBalanceRecord>();
			List<PMProjectBalanceRecord> expense = new List<PMProjectBalanceRecord>();
			List<PMProjectBalanceRecord> offbalance = new List<PMProjectBalanceRecord>();

			PXSelectBase<PMBudget> select = new PXSelectJoinGroupBy<PMBudget,
			InnerJoin<PMAccountGroup, On<PMAccountGroup.groupID, Equal<PMBudget.accountGroupID>>>,
			Where<PMBudget.projectID, Equal<Current<PMProject.contractID>>>,
			Aggregate<GroupBy<PMBudget.accountGroupID,
			Sum<PMBudget.curyAmount,
			Sum<PMBudget.amount,
			Sum<PMBudget.curyRevisedAmount,
			Sum<PMBudget.revisedAmount,
			Sum<PMBudget.curyActualAmount,
			Sum<PMBudget.actualAmount,
			Sum<PMBudget.curyCommittedAmount,
			Sum<PMBudget.committedAmount,
			Sum<PMBudget.curyCommittedOrigAmount,
			Sum<PMBudget.committedOrigAmount,
			Sum<PMBudget.curyCommittedOpenAmount,
			Sum<PMBudget.committedOpenAmount,
			Sum<PMBudget.curyChangeOrderAmount,
			Sum<PMBudget.changeOrderAmount,
			Sum<PMBudget.curyCommittedInvoicedAmount,
			Sum<PMBudget.committedInvoicedAmount,
			Sum<PMBudget.curyDraftChangeOrderAmount>>>>>>>>>>>>>>>>>>>>(this);

			foreach (PXResult<PMBudget, PMAccountGroup> res in select.Select())
			{
				PMBudget ps = (PMBudget)res;
				PMAccountGroup ag = (PMAccountGroup)res;

				if (ag.IsExpense == true)
				{
					expense.Add(BalanceRecordFromBudget(ps, ag));
				}
				else
				{
					switch (ag.Type)
					{
						case AccountType.Asset:
							asset.Add(BalanceRecordFromBudget(ps, ag));
							break;
						case AccountType.Liability:
							liability.Add(BalanceRecordFromBudget(ps, ag));
							break;
						case AccountType.Income:
							income.Add(BalanceRecordFromBudget(ps, ag));
							break;
						case AccountType.Expense:
							expense.Add(BalanceRecordFromBudget(ps, ag));
							break;
						case PMAccountType.OffBalance:
							offbalance.Add(BalanceRecordFromBudget(ps, ag));
							break;
					}
				}
			}

			asset.Sort((x, y) => CompareBalanceRecords(x, y));
			liability.Sort((x, y) => CompareBalanceRecords(x, y));
			income.Sort((x, y) => CompareBalanceRecords(x, y));
			expense.Sort((x, y) => CompareBalanceRecords(x, y));
			offbalance.Sort((x, y) => CompareBalanceRecords(x, y));

			int cx = 0;

			var cache = BalanceRecords.Cache;
			foreach (PMProjectBalanceRecord line in GetBalanceLines(AccountType.Asset, asset))
			{
				line.SortOrder = cx++;
				yield return cache.Locate(line) ?? cache.Insert(line);
			}

			foreach (PMProjectBalanceRecord line in GetBalanceLines(AccountType.Liability, liability))
			{
				line.SortOrder = cx++;
				yield return cache.Locate(line) ?? cache.Insert(line);
			}

			foreach (PMProjectBalanceRecord line in GetBalanceLines(AccountType.Income, income))
			{
				line.SortOrder = cx++;
				yield return cache.Locate(line) ?? cache.Insert(line);
			}

			foreach (PMProjectBalanceRecord line in GetBalanceLines(AccountType.Expense, expense))
			{
				line.SortOrder = cx++;
				yield return cache.Locate(line) ?? cache.Insert(line);
			}

			foreach (PMProjectBalanceRecord line in GetBalanceLines(PMAccountType.OffBalance, offbalance))
			{
				line.SortOrder = cx++;
				yield return cache.Locate(line) ?? cache.Insert(line);
			}

			BalanceRecords.Cache.IsDirty = false;
		}

		private int CompareBalanceRecords(PMProjectBalanceRecord x, PMProjectBalanceRecord y)
		{
			return x.AccountGroup.CompareTo(y.AccountGroup);
		}

		[PXCopyPasteHiddenView]
		public PXSelect<PMBudgetProduction, Where<PMBudgetProduction.projectID, Equal<Current<PMProject.contractID>>>> BudgetProduction;

		[PXCopyPasteHiddenView]
		[PXFilterable]
		public PXSelectJoin<SelectedTask, LeftJoin<PMProject, On<SelectedTask.projectID, Equal<PMProject.contractID>>>, Where<SelectedTask.autoIncludeInPrj, NotEqual<True>, And<SelectedTask.projectID, Equal<Current<PMProject.templateID>>, Or<PMProject.nonProject, Equal<True>>>>> TasksForAddition;

		protected IEnumerable tasksForAddition()
		{
			PXSelectBase<SelectedTask> select = new PXSelectJoin<SelectedTask,
				LeftJoin<PMProject, On<SelectedTask.projectID, Equal<PMProject.contractID>>>,
				Where<SelectedTask.autoIncludeInPrj, NotEqual<True>,
				And<SelectedTask.projectID, Equal<Current<PMProject.templateID>>,
				Or<PMProject.nonProject, Equal<True>>>>>(this);

			List<string> existingTasks = new List<string>();
			foreach (PMTask task in Tasks.Select())
			{
				if (string.IsNullOrEmpty(task.TaskCD))
					continue;

				existingTasks.Add(task.TaskCD.ToUpperInvariant().Trim());
			}

			List<SelectedTask> taskToShow = new List<SelectedTask>();
			foreach (SelectedTask task in select.Select())
			{
				if (!existingTasks.Contains(task.TaskCD.ToUpperInvariant().Trim()))
				{
					taskToShow.Add(task);
				}
			}

			return taskToShow;
		}
		
		[PXCopyPasteHiddenView]
		[PXViewName(Messages.Approval)]
		public EPApprovalAutomation<PMProject, PMProject.approved, PMProject.rejected, PMProject.hold, PMSetupProjectApproval> Approval;

		[PXHidden]
		[PXCopyPasteHiddenView]
		public PXSelect<PMTimeActivity> ActivityDummy;

		[PXFilterable]
		[PXCopyPasteHiddenView]
		[PXViewName(Messages.ChangeOrder)]
		public PXSelect<PMChangeOrder, Where<PMChangeOrder.projectID, Equal<Current<PMProject.contractID>>>> ChangeOrders;

		[PXFilterable]
		[PXCopyPasteHiddenView]
		[PXViewName(PO.Messages.PurchaseOrder)]
		public PXSelectJoinGroupBy<PO.POOrder,
			InnerJoin<PO.POLine, On<PO.POOrder.orderType, Equal<PO.POLine.orderType>,
				And<PO.POOrder.orderNbr, Equal<PO.POLine.orderNbr>,
				And<PO.POLine.projectID, Equal<Current<PMProject.contractID>>>>>>,
			Aggregate<GroupBy<PO.POOrder.orderType,
			GroupBy<PO.POOrder.orderNbr>>>> PurchaseOrders;

		[PXImport(typeof(PMProject))]
		[PXViewName(Messages.UnionLocals)]
		public PXSelect<PMProjectUnion, Where<PMProjectUnion.projectID, Equal<Current<PMProject.contractID>>>> Unions;

		[PXCopyPasteHiddenView]
		public PXSelect<PMQuote> Quote;

		[PXCopyPasteHiddenView]
		public PXSelect<CROpportunityProducts> QuoteDetails;

		[PXHidden]
		[PXCopyPasteHiddenView]
		public PXSelect<PMForecastDetail> ForecastDetails;

		public PXFilter<CopyDialogInfo> CopyDialog;

		public PXFilter<LoadFromTemplateInfo> LoadFromTemplateDialog;

		private bool _isLoadFromTemplate;

		[InjectDependency]
		protected ILicenseLimitsService _licenseLimits { get; set; }

		#endregion

		#region Actions/Buttons

		public PXAction<PMProject> validateAddresses;
		[PXUIField(DisplayName = CS.Messages.ValidateAddress, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, FieldClass = CS.Messages.ValidateAddress)]
		[PXButton]
		public virtual IEnumerable ValidateAddresses(PXAdapter adapter)
		{
			foreach (PMProject current in adapter.Get<PMProject>())
			{
				if (current != null)
				{
					PMAddress address = this.Billing_Address.Select();
					if (address != null && address.IsDefaultAddress == false && address.IsValidated == false)
					{
						PXAddressValidator.Validate<PMAddress>(this, address, true, true);
					}
				}
				yield return current;
			}
		}

		public PXAction<PMProject> bill;
		[PXUIField(DisplayName = Messages.Bill, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Select)]
		[PXProcessButton(Tooltip = Messages.BillTip)]
		public virtual IEnumerable Bill(PXAdapter adapter)
		{
			if (CanBeBilled())
			{
				this.Save.Press();
				int? projectID = Project.Current.ContractID;

				PXLongOperation.StartOperation(this, () => {
					PMBillEngine engine = PXGraph.CreateInstance<PMBillEngine>();
					engine.Clear(PXClearOption.ClearAll);
					engine.SelectTimeStamp();
					var result = engine.Bill(projectID, null, null);

					if (result.IsEmpty)
					{
						throw new PXOperationCompletedException(Warnings.NoPendingValuesToBeBilled, result.BillingDate.ToShortDateString());
					}
					else if (result.IsSingle)
					{
						if (result.Proformas.Count == 1)
						{
							if (!string.IsNullOrEmpty(result.Proformas[0].RefNbr))
							{
								ProformaEntry target = PXGraph.CreateInstance<ProformaEntry>();
								target.Clear(PXClearOption.ClearAll);
								target.SelectTimeStamp();
								target.Document.Current = PXSelect<PMProforma, Where<PMProforma.refNbr, Equal<Required<PMProforma.refNbr>>, And<PMProforma.corrected, NotEqual<True>>>>.Select(target, result.Proformas[0].RefNbr);
								throw new PXRedirectRequiredException(target, true, "ViewInvoice") { Mode = PXBaseRedirectException.WindowMode.Same };

							}
						}
						else if (!string.IsNullOrEmpty(result.Invoices[0].RefNbr))
						{
							ARInvoiceEntry target = PXGraph.CreateInstance<ARInvoiceEntry>();
							target.Clear(PXClearOption.ClearAll);
							target.SelectTimeStamp();
							target.Document.Current = PXSelect<ARInvoice, Where<ARInvoice.docType, Equal<Required<ARInvoice.docType>>, And<ARInvoice.refNbr, Equal<Required<ARInvoice.refNbr>>>>>.Select(target, result.Invoices[0].DocType, result.Invoices[0].RefNbr);
							throw new PXRedirectRequiredException(target, true, "ViewInvoice") { Mode = PXBaseRedirectException.WindowMode.NewWindow };
						}
					}
				});
			}

			return adapter.Get();
		}

		public PXAction<PMProject> createChangeOrder;
		[PXUIField(DisplayName = "Create Change Order", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		protected virtual IEnumerable CreateChangeOrder(PXAdapter adapter)
		{
			if (Project.Current != null)
			{
				ChangeOrderEntry target = PXGraph.CreateInstance<ChangeOrderEntry>();
				target.Document.Insert();
				target.Document.SetValueExt<PMChangeOrder.projectID>(target.Document.Current, Project.Current.ContractID);

				throw new PXRedirectRequiredException(target, false, Messages.ChangeOrder) { Mode = PXBaseRedirectException.WindowMode.Same };
			}
			return adapter.Get();
		}

		public PXAction<PMProject> laborCostRates;
		[PXUIField(DisplayName = "Labor Cost Rates", MapEnableRights = PXCacheRights.Select)]
		[PXButton]
		protected virtual IEnumerable LaborCostRates(PXAdapter adapter)
		{
			if (Project.Current != null)
			{
				LaborCostRateMaint graph = PXGraph.CreateInstance<LaborCostRateMaint>();
				graph.Filter.Current.ProjectID = Project.Current.ContractID;
				graph.Filter.Select();
				throw new PXRedirectRequiredException(graph, "Labor Cost Rates");
			}
			return adapter.Get();
		}

		public PXAction<PMProject> forecast;
		[PXUIField(DisplayName = "Project Budget Forecast", MapEnableRights = PXCacheRights.Select)]
		[PXButton]
		protected virtual IEnumerable Forecast(PXAdapter adapter)
		{
			if (Project.Current != null)
			{
				ForecastMaint graph = PXGraph.CreateInstance<ForecastMaint>();
				var select = new PXSelect<PMForecast, Where<PMForecast.projectID, Equal<Current<PMProject.contractID>>>, OrderBy<Desc<PMForecast.lastModifiedDateTime>>>(this);
				PMForecast first = select.Select();
				if (first != null)
				{
					graph.Revisions.Current = first;
				}
				else
				{
					graph.Revisions.Insert();
					graph.Revisions.Current.ProjectID = Project.Current.ContractID;
					graph.Revisions.Cache.IsDirty = false;
				}
											
				throw new PXRedirectRequiredException(graph, "Project Budget Forecast");
			}
			return adapter.Get();
		}

		public PXAction<PMProject> projectBalanceReport;
		[PXUIField(DisplayName = "Print Project Balance", MapEnableRights = PXCacheRights.Select)]
		[PXButton(SpecialType = PXSpecialButtonType.Report)]
		protected virtual IEnumerable ProjectBalanceReport(PXAdapter adapter)
		{
			if (Project.Current != null)
			{				
				var parameters = new Dictionary<string, string>();
				parameters["ProjectID"] = Project.Current.ContractCD;
				throw new PXReportRequiredException(parameters, "PM621000", "PM621000");
			}

			return adapter.Get();
		}

		public PXAction<PMProject> currencyRates;
		[PXUIField(DisplayName = "Print Currency Rates", MapEnableRights = PXCacheRights.Select)]
		[PXButton(SpecialType = PXSpecialButtonType.Report)]
		protected virtual IEnumerable CurrencyRates(PXAdapter adapter)
		{
			if (Project.Current != null)
			{
				var parameters = new Dictionary<string, string>();
				parameters["StartDate"] = Project.Current.StartDate.ToString();
				if (Project.Current.ExpireDate != null)
				{
					parameters["EndDate"] = Project.Current.ExpireDate.ToString();
				}
				parameters["RateType"] = Project.Current.RateTypeID ?? CMSetup.Current.PMRateTypeDflt;
				throw new PXReportRequiredException(parameters, "CM650500", PXBaseRedirectException.WindowMode.NewWindow, "CM650500");
			}

			return adapter.Get();
		}

		public PXAction<PMProject> setCurrencyRates;
		[PXUIField(DisplayName = "Set Rates", MapEnableRights = PXCacheRights.Select)]
		[PXButton]
		protected virtual IEnumerable SetCurrencyRates(PXAdapter adapter)
		{
			if (Project.Current != null)
			{
				CM.CuryRateMaint target = PXGraph.CreateInstance<CM.CuryRateMaint>();
				target.Clear();
				target.Filter.Current.ToCurrency = Project.Current.CuryIDCopy;
				throw new PXRedirectRequiredException(target, true, "Set Rates");
			}

			return adapter.Get();
		}

		public PXAction<PMProject> viewTask;
		[PXUIField(DisplayName = Messages.ViewTask, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.DataEntry)]
		public IEnumerable ViewTask(PXAdapter adapter)
		{
			if (Tasks.Current != null && Project.Cache.GetStatus(this.Project.Current) != PXEntryStatus.Inserted)
			{
				if (Tasks.Cache.GetStatus(Tasks.Current) == PXEntryStatus.Inserted || Tasks.Cache.GetStatus(Tasks.Current) == PXEntryStatus.Updated)
				{
					this.Save.Press();
				}

				ProjectTaskEntry graph = PXGraph.CreateInstance<ProjectTaskEntry>();
				graph.Task.Current = PMTask.PK.FindDirty(this, Tasks.Current.ProjectID, Tasks.Current.TaskID);

				throw new PXPopupRedirectException(graph, Messages.ProjectTaskEntry + " - " + Messages.ViewTask, true);
			}
			return adapter.Get();
		}		

		public PXAction<PMProject> viewRevenueBudgetInventory;
		[PXUIField(DisplayName = "View Inventory Item", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable ViewRevenueBudgetInventory(PXAdapter adapter)
		{
			InventoryItem item = PXSelect<InventoryItem, Where<InventoryItem.inventoryID, Equal<Current<PMRevenueBudget.inventoryID>>>>.Select(this);
			if (item.ItemStatus != InventoryItemStatus.Unknown)
			{
				if (item.StkItem == true)
				{
					InventoryItemMaint graph = CreateInstance<InventoryItemMaint>();
					graph.Item.Current = item;
					throw new PXRedirectRequiredException(graph, true, "View Inventory Item") { Mode = PXBaseRedirectException.WindowMode.NewWindow };
				}
				else
				{
					NonStockItemMaint graph = CreateInstance<NonStockItemMaint>();
					graph.Item.Current = item;
					throw new PXRedirectRequiredException(graph, true, "View Inventory Item") { Mode = PXBaseRedirectException.WindowMode.NewWindow };
				}
			}
			return adapter.Get();
		}

		public PXAction<PMProject> viewCostBudgetInventory;
		[PXUIField(DisplayName = "View Inventory Item", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable ViewCostBudgetInventory(PXAdapter adapter)
		{
			InventoryItem item = PXSelect<InventoryItem, Where<InventoryItem.inventoryID, Equal<Current<PMCostBudget.inventoryID>>>>.Select(this);
			if (item.ItemStatus != InventoryItemStatus.Unknown)
			{
				if (item.StkItem == true)
				{
					InventoryItemMaint graph = CreateInstance<InventoryItemMaint>();
					graph.Item.Current = item;
					throw new PXRedirectRequiredException(graph, true, "View Inventory Item") { Mode = PXBaseRedirectException.WindowMode.NewWindow };
				}
				else
				{
					NonStockItemMaint graph = CreateInstance<NonStockItemMaint>();
					graph.Item.Current = item;
					throw new PXRedirectRequiredException(graph, true, "View Inventory Item") { Mode = PXBaseRedirectException.WindowMode.NewWindow };
				}
			}
			return adapter.Get();
		}

		public PXAction<PMProject> viewCostCommitments;
		[PXUIField(DisplayName = Messages.ViewCommitments, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.DataEntry)]
		public IEnumerable ViewCostCommitments(PXAdapter adapter)
		{
			if (CostBudget.Current != null)
			{
				CommitmentInquiry graph = PXGraph.CreateInstance<CommitmentInquiry>();
				graph.Filter.Current.AccountGroupID = CostBudget.Current.AccountGroupID;
				graph.Filter.Current.ProjectID = CostBudget.Current.ProjectID;
				graph.Filter.Current.ProjectTaskID = CostBudget.Current.ProjectTaskID;
				graph.Filter.Current.CostCode = CostBudget.Current.CostCodeID;

				throw new PXPopupRedirectException(graph, Messages.CommitmentEntry + " - " + Messages.ViewCommitments, true);
			}
			return adapter.Get();
		}

		public PXAction<PMProject> viewBalanceTransactions;
		[PXUIField(DisplayName = Messages.ViewTransactions, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.DataEntry)]
		public IEnumerable ViewBalanceTransactions(PXAdapter adapter)
		{
			if (BalanceRecords.Current != null && BalanceRecords.Current.RecordID > 0)
			{
				TransactionInquiry target = PXGraph.CreateInstance<TransactionInquiry>();
				target.Filter.Insert(new TransactionInquiry.TranFilter());
				target.Filter.Current.ProjectID = Project.Current.ContractID;
				target.Filter.Current.AccountGroupID = BalanceRecords.Current.RecordID;
				target.Filter.Current.IncludeUnreleased = false;

				throw new PXPopupRedirectException(target, Messages.TransactionInquiry + " - " + Messages.ViewTransactions, true);
			}
			return adapter.Get();
		}

		public PXAction<PMProject> viewCommitments;
		[PXUIField(DisplayName = Messages.ViewCommitments, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.DataEntry)]
		public IEnumerable ViewCommitments(PXAdapter adapter)
		{
			if (BalanceRecords.Current != null && BalanceRecords.Current.RecordID > 0)
			{
				CommitmentInquiry graph = PXGraph.CreateInstance<CommitmentInquiry>();
				graph.Filter.Current.AccountGroupID = BalanceRecords.Current.RecordID;
				graph.Filter.Current.ProjectID = Project.Current.ContractID;


				throw new PXPopupRedirectException(graph, Messages.CommitmentEntry + " - " + Messages.ViewCommitments, true);
			}
			return adapter.Get();
		}


		public PXAction<PMProject> viewInvoice;
		[PXUIField(DisplayName = CT.Messages.ViewInvoice, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Enabled = true)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.Inquiry)]
		public virtual IEnumerable ViewInvoice(PXAdapter adapter)
		{
			if (Invoices.Current != null && !string.IsNullOrEmpty(Invoices.Current.ARRefNbr))
			{
				NavigateInvoice(Invoices.Current.ARDocType, Invoices.Current.ARRefNbr, PXBaseRedirectException.WindowMode.NewWindow);
			}

			return adapter.Get();
		}

		public PXAction<PMProject> viewOrigDocument;
		[PXUIField(DisplayName = CT.Messages.ViewInvoice, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Enabled = true)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.Inquiry)]
		public virtual IEnumerable ViewOrigDocument(PXAdapter adapter)
		{
			ARInvoice doc = PXSelect<ARInvoice, Where<ARInvoice.docType, Equal<Required<PMBillingRecord.aRDocType>>, And<ARInvoice.refNbr, Equal<Required<PMBillingRecord.aRRefNbr>>>>>.Select(this, Invoices.Current.ARDocType, Invoices.Current.ARRefNbr);
			ARInvoice origDoc = PXSelect<ARInvoice, Where<ARInvoice.refNbr, Equal<Required<PMBillingRecord.aRRefNbr>>>>.Select(this, doc.OrigRefNbr);
			if (Invoices.Current != null && !string.IsNullOrEmpty(doc.OrigRefNbr))
			{
				NavigateInvoice(origDoc.DocType, doc.OrigRefNbr, PXBaseRedirectException.WindowMode.NewWindow);
			}

			return adapter.Get();
		}

		public virtual void NavigateInvoice(string doctype, string refNbr, PXBaseRedirectException.WindowMode windowMode)
		{
			if (!string.IsNullOrEmpty(refNbr))
			{
				ARInvoiceEntry target = PXGraph.CreateInstance<ARInvoiceEntry>();
				target.Clear(PXClearOption.ClearAll);
				target.SelectTimeStamp();
				target.Document.Current = PXSelect<ARInvoice, Where<ARInvoice.docType, Equal<Required<ARInvoice.docType>>, And<ARInvoice.refNbr, Equal<Required<ARInvoice.refNbr>>>>>.Select(this, doctype, refNbr);
				throw new PXRedirectRequiredException(target, true, "ViewInvoice") { Mode = windowMode };
			}
		}

		public PXAction<PMProject> viewProforma;
		[PXUIField(DisplayName = Messages.ViewProforma, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Enabled = true)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.Inquiry)]
		public virtual IEnumerable ViewProforma(PXAdapter adapter)
		{
			if (Invoices.Current != null && !string.IsNullOrEmpty(Invoices.Current.ProformaRefNbr))
			{
				NavigateProforma(Invoices.Current.ProformaRefNbr, PXBaseRedirectException.WindowMode.NewWindow);
			}

			return adapter.Get();
		}

		public virtual void NavigateProforma(string refNbr, PXBaseRedirectException.WindowMode windowMode)
		{
			if (!string.IsNullOrEmpty(refNbr))
			{
				ProformaEntry target = PXGraph.CreateInstance<ProformaEntry>();
				target.Clear(PXClearOption.ClearAll);
				target.SelectTimeStamp();
				target.Document.Current = PXSelect<PMProforma, Where<PMProforma.refNbr, Equal<Required<PMProforma.refNbr>>, And<PMProforma.corrected, NotEqual<True>>>>.Select(this, refNbr);
				throw new PXRedirectRequiredException(target, true, "ViewInvoice") { Mode = windowMode };
			}
		}

		public PXAction<PMProject> viewChangeOrder;
		[PXUIField(DisplayName = Messages.ViewChangeOrder, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Enabled = true)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.Inquiry)]
		public virtual IEnumerable ViewChangeOrder(PXAdapter adapter)
		{
			if (ChangeOrders.Current != null)
			{
				ChangeOrderEntry target = PXGraph.CreateInstance<ChangeOrderEntry>();
				target.Clear(PXClearOption.ClearAll);
				target.SelectTimeStamp();
				target.Document.Current = PMChangeOrder.PK.Find(this, ChangeOrders.Current.RefNbr);
				throw new PXRedirectRequiredException(target, true, "ViewInvoice") { Mode = PXBaseRedirectException.WindowMode.NewWindow };

			}

			return adapter.Get();
		}

		public PXAction<PMProject> viewOrigChangeOrder;
		[PXUIField(DisplayName = Messages.ViewChangeOrder, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.DataEntry)]

		public virtual IEnumerable ViewOrigChangeOrder(PXAdapter adapter)
		{
			if (ChangeOrders.Current != null && ChangeOrders.Current.OrigRefNbr != null)
			{

				ChangeOrderEntry target = PXGraph.CreateInstance<ChangeOrderEntry>();
				target.Clear(PXClearOption.ClearAll);
				target.SelectTimeStamp();
				target.Document.Current = PMChangeOrder.PK.Find(this, ChangeOrders.Current.OrigRefNbr);
				throw new PXRedirectRequiredException(target, true, "View Change Order") { Mode = PXBaseRedirectException.WindowMode.NewWindow };

			}
			return adapter.Get();
		}

		public PXAction<PMProject> viewPurchaseOrder;
		[PXUIField(DisplayName = Messages.ViewPurchaseOrder, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Enabled = true)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.Inquiry)]
		public virtual IEnumerable ViewPurchaseOrder(PXAdapter adapter)
		{
			if (PurchaseOrders.Current != null)
			{
				PO.POOrderEntry target = CreatePOEntryGraph(PurchaseOrders.Current);
				target.Document.Current = PurchaseOrders.Current;
				throw new PXRedirectRequiredException(target, true, Messages.ViewPurchaseOrder) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
			}

			return adapter.Get();
		}

		public virtual PO.POOrderEntry CreatePOEntryGraph(PO.POOrder order)
		{
			return PXGraph.CreateInstance<PO.POOrderEntry>();
		}

		public PXAction<PMProject> addTasks;
		[PXUIField(DisplayName = Messages.AddCommonTasks, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Select, Enabled = false)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.Process)]
		public virtual IEnumerable AddTasks(PXAdapter adapter)
		{
			foreach (SelectedTask task in TasksForAddition.Cache.Updated)
			{
				if (task.Selected == true)
				{
					CopyTask(task, (int)ProjectProperties.Current.ContractID, DefaultFromTemplateSettings.Default);
					task.Selected = false;
				}
			}
			return adapter.Get();
		}

		[PXUIField]
		[PXDeleteButton(ConfirmationMessage = ActionsMessages.ConfirmDeleteExplicit)]
		public virtual IEnumerable delete(PXAdapter adapter)
		{
			PXLongOperation.StartOperation(this, delegate ()
			{
				ProjectEntry pe = PXGraph.CreateInstance<ProjectEntry>();
				pe.Project.Current = (PMProject)Project.Cache.CreateCopy(Project.Current);
				pe.Project.Delete(pe.Project.Current);
				pe.Save.Press();
			});

			return adapter.Get();
		}


		public PXAction<PMProject> activateTasks;
		[PXUIField(DisplayName = Messages.ActivateTasks, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Select, Enabled = false)]
		[PXButton]
		public virtual IEnumerable ActivateTasks(PXAdapter adapter)
		{
			bool failed = false;
			foreach (PMTask task in Tasks.Select())
			{
				if (task.Status == ProjectTaskStatus.Planned)
				{
					try
					{
						task.Status = ProjectTaskStatus.Active;
						Tasks.Update(task);
					}
					catch (PXSetPropertyException ex)
					{
						failed = true;
						Tasks.Cache.RaiseExceptionHandling<PMTask.status>(task, task.Status, ex);
					}
				}
			}

			if (failed)
			{
				throw new PXException(Messages.AtleastOneTaskWasNotActivated);
			}

			return adapter.Get();
		}

		public PXAction<PMProject> completeTasks;
		[PXUIField(DisplayName = Messages.CompleteTasks, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable CompleteTasks(PXAdapter adapter)
		{

			foreach (PMTask task in Tasks.Select())
			{
				if (task.Status == ProjectTaskStatus.Active)
				{
					task.Status = ProjectTaskStatus.Completed;
					Tasks.Update(task);
				}
			}

			return adapter.Get();
		}

		public PXAction<PMProject> createTemplate;
		[PXUIField(DisplayName = Messages.CreateTemplate, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Select, Enabled = false)]
		[PXButton]
		public virtual IEnumerable CreateTemplate(PXAdapter adapter)
		{
			PMProject templ = new PMProject();

			if (!DimensionMaint.IsAutonumbered(this, ProjectAttribute.DimensionNameTemplate))
			{
				if (TemplateSettings.AskExt() == WebDialogResult.OK && !string.IsNullOrEmpty(TemplateSettings.Current.TemplateID))
				{
					templ.ContractCD = TemplateSettings.Current.TemplateID;
				}
				else
				{
					return adapter.Get();
				}
			}

			this.Save.Press();
			PMProject project = Project.Current;

			TemplateMaint graph = CreateInstance<TemplateMaint>();
			templ = graph.Project.Insert(templ);

			templ.Description = project.Description;
			PXDBLocalizableStringAttribute.CopyTranslations<PMProject.description, PMProject.description>
				(Caches[typeof(PMProject)], project, Caches[typeof(PMProject)], templ);
			templ.BudgetLevel = project.BudgetLevel;
			templ.CostBudgetLevel = project.CostBudgetLevel;
			templ.CreateProforma = project.CreateProforma;
			templ.PrepaymentEnabled = project.PrepaymentEnabled;
			templ.PrepaymentDefCode = project.PrepaymentDefCode;
			templ.LimitsEnabled = project.LimitsEnabled;
			templ.ChangeOrderWorkflow = project.ChangeOrderWorkflow;
			templ.TermsID = project.TermsID;
			templ.AutoAllocate = project.AutoAllocate;
			templ.AutomaticReleaseAR = project.AutomaticReleaseAR;
			templ.DefaultSalesAccountID = project.DefaultSalesAccountID;
			templ.DefaultSalesSubID = project.DefaultSalesSubID;
			templ.DefaultExpenseAccountID = project.DefaultExpenseAccountID;
			templ.DefaultExpenseSubID = project.DefaultExpenseSubID;
			templ.DefaultAccrualAccountID = project.DefaultAccrualAccountID;
			templ.DefaultAccrualSubID = project.DefaultAccrualSubID;
			templ.DefaultBranchID = project.DefaultBranchID;
			templ.CalendarID = project.CalendarID;
			templ.RestrictToEmployeeList = project.RestrictToEmployeeList;
			templ.RestrictToResourceList = project.RestrictToResourceList;
			templ.CuryID = project.CuryID;
			templ.CuryIDCopy = project.CuryIDCopy;
			templ.AllowOverrideCury = project.AllowOverrideCury;
			templ.AllowOverrideRate = project.AllowOverrideRate;
			templ.AllocationID = project.AllocationID;
			templ.BillingID = project.BillingID;
			templ.ApproverID = project.ApproverID;
			templ.OwnerID = project.OwnerID;
			templ.RateTableID = project.RateTableID;
			templ.RetainagePct = project.RetainagePct;
			templ.IncludeCO = project.IncludeCO;
			templ.RetainageMaxPct = project.RetainageMaxPct;
			templ.RetainageMode = project.RetainageMode;
			templ.SteppedRetainage = project.SteppedRetainage;

			templ.VisibleInAP = project.VisibleInAP;
			templ.VisibleInGL = project.VisibleInGL;
			templ.VisibleInAR = project.VisibleInAR;
			templ.VisibleInSO = project.VisibleInSO;
			templ.VisibleInPO = project.VisibleInPO;
			templ.VisibleInTA = project.VisibleInTA;
			templ.VisibleInEA = project.VisibleInEA;
			templ.VisibleInIN = project.VisibleInIN;
			templ.VisibleInCA = project.VisibleInCA;
			templ.VisibleInCR = project.VisibleInCR;

			graph.Answers.CopyAllAttributes(templ, project);

			ContractBillingSchedule billing = PXSelect<ContractBillingSchedule, Where<ContractBillingSchedule.contractID, Equal<Current<PMProject.contractID>>>>.SelectSingleBound(this, new object[] { project });
			if (billing != null)
			{
				graph.Billing.Current.Type = billing.Type;
			}

			Dimension skey = PXSelect<Dimension, Where<Dimension.dimensionID, Equal<Required<Dimension.dimensionID>>>>.Select(this, ProjectTaskAttribute.DimensionName);
			bool isAutoNumbered = skey != null && skey.NumberingID != null;
			Dictionary<int, int> taskIDs = new Dictionary<int, int>();
			List<PMCostBudget> costBudgetRecords = new List<PMCostBudget>();
			foreach (PMTask task in PXSelect<PMTask, Where<PMTask.projectID, Equal<Current<PMProject.contractID>>>>.SelectMultiBound(this, new object[] { project }))
			{
				PMTask dst = graph.Tasks.Insert(new PMTask { TaskCD = !isAutoNumbered ? task.TaskCD : null, ProjectID = templ.ContractID });
				dst.BillingID = task.BillingID;
				dst.AllocationID = task.AllocationID;
				dst.Description = task.Description;
				PXDBLocalizableStringAttribute.CopyTranslations<PMTask.description, PMTask.description>
					(Caches[typeof(PMTask)], task, Caches[typeof(PMTask)], dst);
				dst.ApproverID = task.ApproverID;
				dst.TaxCategoryID = task.TaxCategoryID;
				dst.BillingOption = task.BillingOption;
				dst.DefaultSalesAccountID = task.DefaultSalesAccountID;
				dst.DefaultSalesSubID = task.DefaultSalesSubID;
				dst.DefaultExpenseAccountID = task.DefaultExpenseAccountID;
				dst.DefaultExpenseSubID = task.DefaultExpenseSubID;
				dst.DefaultAccrualAccountID = task.DefaultAccrualAccountID;
				dst.DefaultAccrualSubID = task.DefaultAccrualSubID;
				dst.DefaultBranchID = task.DefaultBranchID;
				dst.WipAccountGroupID = task.WipAccountGroupID;
				dst.BillSeparately = task.BillSeparately;
				dst.VisibleInGL = task.VisibleInGL;
				dst.VisibleInAP = task.VisibleInAP;
				dst.VisibleInAR = task.VisibleInAR;
				dst.VisibleInSO = task.VisibleInSO;
				dst.VisibleInPO = task.VisibleInPO;
				dst.VisibleInTA = task.VisibleInTA;
				dst.VisibleInEA = task.VisibleInEA;
				dst.VisibleInIN = task.VisibleInIN;
				dst.VisibleInCA = task.VisibleInCA;
				dst.VisibleInCR = task.VisibleInCR;
				dst.IsActive = task.IsActive ?? false;
				dst.CompletedPctMethod = task.CompletedPctMethod;
				dst.RateTableID = task.RateTableID;
				dst.IsDefault = task.IsDefault;
				dst.Type = task.Type;

				graph.TaskAnswers.CopyAllAttributes(dst, task);
				taskIDs.Add(task.TaskID.Value, dst.TaskID.Value);
								
				var selectCostBudget = new PXSelect<PMCostBudget,
				Where<PMCostBudget.projectID, Equal<Required<PMCostBudget.projectID>>,
				And<PMCostBudget.projectTaskID, Equal<Required<PMCostBudget.projectTaskID>>,
				And<PMCostBudget.type, Equal<GL.AccountType.expense>>>>>(this);

				foreach (PMCostBudget budget in selectCostBudget.Select(task.ProjectID, task.TaskID))
				{
					budget.ProjectID = dst.ProjectID;
					budget.ProjectTaskID = dst.TaskID;
					budget.ActualAmount = 0;
					budget.CuryActualAmount = 0;
					budget.ActualQty = 0;
					budget.CuryAmountToInvoice = 0;
					budget.QtyToInvoice = 0;
					budget.CuryDraftChangeOrderAmount = 0;
					budget.DraftChangeOrderAmount = 0;
					budget.DraftChangeOrderQty = 0;
					budget.CuryChangeOrderAmount = 0;
					budget.ChangeOrderAmount = 0;
					budget.ChangeOrderQty = 0;
					budget.CuryCommittedOrigAmount = 0;
					budget.CommittedOrigAmount = 0;
					budget.CommittedOrigQty = 0;
					budget.CuryCommittedAmount = 0;
					budget.CommittedAmount = 0;
					budget.CuryCommittedInvoicedAmount = 0;
					budget.CuryCommittedOpenAmount = 0;
					budget.CommittedOpenQty = 0;
					budget.CommittedQty = 0;
					budget.CommittedReceivedQty = 0;
					budget.CompletedPct = 0;
					budget.CuryCostAtCompletion = 0;
					budget.CostAtCompletion = 0;
					budget.CuryCostToComplete = 0;
					budget.CostToComplete = 0;
					budget.CuryInvoicedAmount = 0;
					budget.CuryLastCostAtCompletion = 0;
					budget.LastCostAtCompletion = 0;
					budget.CuryLastCostToComplete = 0;
					budget.LastCostToComplete = 0;
					budget.LastPercentCompleted = 0;
					budget.PercentCompleted = 0;
					budget.CuryPrepaymentInvoiced = 0;
					budget.PrepaymentInvoiced = 0;
					budget.CuryDraftRetainedAmount = 0;
					budget.DraftRetainedAmount = 0;
					budget.CuryRetainedAmount = 0;
					budget.RetainedAmount = 0;
					budget.CuryTotalRetainedAmount = 0;
					budget.TotalRetainedAmount = 0;
					budget.NoteID = null;

					costBudgetRecords.Add(budget);
				}

				var selectRevenueBudget = new PXSelect<PMRevenueBudget,
					Where<PMRevenueBudget.projectID, Equal<Required<PMRevenueBudget.projectID>>,
					And<PMRevenueBudget.projectTaskID, Equal<Required<PMRevenueBudget.projectTaskID>>,
					And<PMRevenueBudget.type, Equal<GL.AccountType.income>>>>>(this);

				foreach (PMRevenueBudget budget in selectRevenueBudget.Select(task.ProjectID, task.TaskID))
				{
					budget.ProjectID = dst.ProjectID;
					budget.ProjectTaskID = dst.TaskID;
					budget.CuryActualAmount = 0;
					budget.ActualAmount = 0;
					budget.ActualQty = 0;
					budget.CuryAmountToInvoice = 0;
					budget.QtyToInvoice = 0;
					budget.CuryDraftChangeOrderAmount = 0;
					budget.DraftChangeOrderAmount = 0;
					budget.DraftChangeOrderQty = 0;
					budget.CuryChangeOrderAmount = 0;
					budget.ChangeOrderAmount = 0;
					budget.ChangeOrderQty = 0;
					budget.CuryCommittedOrigAmount = 0;
					budget.CommittedOrigAmount = 0;
					budget.CommittedOrigQty = 0;
					budget.CuryCommittedAmount = 0;
					budget.CommittedAmount = 0;
					budget.CuryCommittedInvoicedAmount = 0;
					budget.CuryCommittedOpenAmount = 0;
					budget.CommittedOpenQty = 0;
					budget.CommittedQty = 0;
					budget.CommittedReceivedQty = 0;
					budget.CompletedPct = 0;
					budget.CuryCostAtCompletion = 0;
					budget.CostAtCompletion = 0;
					budget.CuryCostToComplete = 0;
					budget.CostToComplete = 0;
					budget.CuryInvoicedAmount = 0;
					budget.CuryLastCostAtCompletion = 0;
					budget.LastCostAtCompletion = 0;
					budget.CuryLastCostToComplete = 0;
					budget.LastCostToComplete = 0;
					budget.LastPercentCompleted = 0;
					budget.PercentCompleted = 0;
					budget.CuryPrepaymentInvoiced = 0;
					budget.PrepaymentInvoiced = 0;
					budget.CuryDraftRetainedAmount = 0;
					budget.DraftRetainedAmount = 0;
					budget.CuryRetainedAmount = 0;
					budget.RetainedAmount = 0;
					budget.CuryTotalRetainedAmount = 0;
					budget.TotalRetainedAmount = 0;
					budget.NoteID = null;
					graph.RevenueBudget.Insert(budget);
				}

				foreach (PMRecurringItem detail in PXSelect<PMRecurringItem, Where<PMRecurringItem.projectID, Equal<Required<PMTask.projectID>>, And<PMRecurringItem.taskID, Equal<Required<PMTask.taskID>>>>>.Select(this, task.ProjectID, task.TaskID))
				{
					PMRecurringItem newDetail = new PMRecurringItem();
					newDetail.ProjectID = dst.ProjectID;
					newDetail.TaskID = dst.TaskID;
					newDetail.InventoryID = detail.InventoryID;
					newDetail.UOM = detail.UOM;
					newDetail.Description = detail.Description;
					newDetail.Amount = detail.Amount;
					newDetail.AccountSource = detail.AccountSource;
					newDetail.SubMask = detail.SubMask;
					newDetail.AccountID = detail.AccountID;
					newDetail.SubID = detail.SubID;
					newDetail.ResetUsage = detail.ResetUsage;
					newDetail.Included = detail.Included;

					graph.BillingItems.Insert(newDetail);
				}
			}

			OnCreateTemplateTasksInserted(graph, templ, taskIDs);

			foreach (EPEmployeeContract rate in PXSelect<EPEmployeeContract, Where<EPEmployeeContract.contractID, Equal<Current<PMProject.contractID>>>>.SelectMultiBound(this, new object[] { project }))
			{
				EPEmployeeContract dst = graph.EmployeeContract.Insert(new EPEmployeeContract());
				dst.EmployeeID = rate.EmployeeID;
			}

			foreach (EPContractRate rate in PXSelect<EPContractRate, Where<EPContractRate.contractID, Equal<Current<PMProject.contractID>>>>.SelectMultiBound(this, new object[] { project }))
			{
				EPContractRate dst = graph.ContractRates.Insert(new EPContractRate());
				dst.IsActive = rate.IsActive;
				dst.EmployeeID = rate.EmployeeID;
				dst.EarningType = rate.EarningType;
				dst.LabourItemID = rate.LabourItemID;
			}

			foreach (EPEquipmentRate equipment in PXSelect<EPEquipmentRate, Where<EPEquipmentRate.projectID, Equal<Current<PMProject.contractID>>>>.SelectMultiBound(this, new object[] { project }))
			{
				EPEquipmentRate dst = graph.EquipmentRates.Insert(new EPEquipmentRate());
				dst.IsActive = equipment.IsActive;
				dst.EquipmentID = equipment.EquipmentID;
				dst.RunRate = equipment.RunRate;
				dst.SuspendRate = equipment.SuspendRate;
				dst.SetupRate = equipment.SetupRate;
			}

			foreach (PMAccountTask acc in PXSelect<PMAccountTask, Where<PMAccountTask.projectID, Equal<Current<PMProject.contractID>>>>.SelectMultiBound(this, new object[] { project }))
			{
				if (taskIDs.ContainsKey(acc.TaskID.GetValueOrDefault()))
				{
					PMAccountTask dst = (PMAccountTask)graph.Accounts.Cache.Insert();
					dst.ProjectID = templ.ContractID;
					dst.AccountID = acc.AccountID;
					dst.TaskID = taskIDs[acc.TaskID.GetValueOrDefault()];
				}
			}

			foreach (PMCostBudget budget in costBudgetRecords)
			{
				int? pendingRevenueItem = null;				

				if (budget.RevenueTaskID != null)
				{
					if (taskIDs.ContainsKey(budget.RevenueTaskID.Value))
					{
						pendingRevenueItem = budget.RevenueInventoryID;
						budget.RevenueTaskID = taskIDs[budget.RevenueTaskID.Value];
						budget.RevenueInventoryID = null;
					}
					else
					{
						budget.RevenueTaskID = null;
						budget.RevenueInventoryID = null;
					}
				}

				var newBudget = graph.CostBudget.Insert(budget);

				if (pendingRevenueItem != null)
				{
					CostBudget.Cache.SetValue<PMCostBudget.revenueInventoryID>(newBudget, pendingRevenueItem);
				}
			}

			foreach (PMRetainageStep step in RetainageSteps.Select())
			{
				step.ProjectID = templ.ContractID;
				step.NoteID = null;
				graph.RetainageSteps.Insert(step);
			}

			//Delete Existing/Added automaticaly on Template creation:
			foreach (NotificationSource source in graph.NotificationSources.Select())
			{
				foreach (NotificationRecipient recipient in graph.NotificationRecipients.Select(source.SourceID))
				{
					graph.NotificationRecipients.Delete(recipient);
				}
				graph.NotificationSources.Delete(source);
			}

			//Copy from current project
			foreach (NotificationSource source in NotificationSources.Select())
			{
				int? sourceID = source.SourceID;
				source.SourceID = null;
				source.RefNoteID = null;
				NotificationSource newsource = graph.NotificationSources.Insert(source);

				foreach (NotificationRecipient recipient in NotificationRecipients.Select(sourceID))
				{
					if (recipient.ContactType == NotificationContactType.Primary || recipient.ContactType == NotificationContactType.Employee)
					{
						recipient.NotificationID = null;
						recipient.SourceID = newsource.SourceID;
						recipient.RefNoteID = null;
						
						graph.NotificationRecipients.Insert(recipient);
					}
				}
			}
			
			throw new PXRedirectRequiredException(graph, "ProjectTemplate") { Mode = PXBaseRedirectException.WindowMode.Same };
		}

		public PXAction<PMProject> runAllocation;
		[PXUIField(DisplayName = Messages.RunAllocation, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Select)]
		[PXButton(Tooltip = Messages.RunAllocation)]
		public virtual IEnumerable RunAllocation(PXAdapter adapter)
		{
			if (Project.Current != null)
			{
				this.Save.Press();

				PXLongOperation.StartOperation(this, delegate ()
				{
					PMAllocator graph = PXGraph.CreateInstance<PMAllocator>();
					AllocationProcessByProject.Run(graph, Project.Current, Accessinfo.BusinessDate, null, null);
				});
				
			}

			return adapter.Get();
		}

		public PXAction<PMProject> validateBalance;
		[PXUIField(DisplayName = Messages.ValidateBalance, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Select)]
		[PXButton(Tooltip = Messages.ValidateBalance)]
		public virtual IEnumerable ValidateBalance(PXAdapter adapter)
		{
			if (Project.Current != null)
			{
				this.Save.Press();

				PXLongOperation.StartOperation(this, delegate ()
				{
					ProjectBalanceValidationProcess graph = PXGraph.CreateInstance<ProjectBalanceValidationProcess>();
					graph.RunProjectBalanceVerification(Project.Current, 
						new PMValidationFilter() { RebuildCommitments = true, RecalculateChangeOrders = true, RecalculateDraftInvoicesAmount = true, RecalculateUnbilledSummary = true });

				});
				
			}

			return adapter.Get();
		}

		public PXAction<PMProject> autoBudget;
		[PXUIField(DisplayName = Messages.AutoBudget, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Select)]
		[PXButton(Tooltip = Messages.AutoBudgetTip)]
		public virtual IEnumerable AutoBudget(PXAdapter adapter)
		{
			if (Project.Current != null)
			{
				this.Save.Press();

				PXLongOperation.StartOperation(this, delegate ()
				{
					ProjectEntry pe = PXGraph.CreateInstance<ProjectEntry>();
					pe.Project.Current = Project.Current;
					pe.RunAutoBudget();
				});
			}

			return adapter.Get();
		}

		public virtual void RunAutoBudget()
		{
			if (Project.Current != null)
			{
				AutoBudgetWorkerProcess worker = PXGraph.CreateInstance<AutoBudgetWorkerProcess>();
				List<AutoBudgetWorkerProcess.Balance> list = worker.Run(Project.Current.ContractID);

				foreach(AutoBudgetWorkerProcess.Balance balance in list )
				{					
					bool budgetUpdated = false;
					foreach (PMRevenueBudget budget in RevenueBudget.Select())
					{						
						if ( budget.TaskID == balance.TaskID && budget.AccountGroupID  == balance.AccountGroupID && budget.InventoryID == balance.InventoryID && budget.CostCodeID == balance.CostCodeID)
						{
							budgetUpdated = true;
							budget.Qty = balance.Quantity;
							budget.CuryAmount = balance.Amount;
							RevenueBudget.Update(budget);
						}
					}

					if (!budgetUpdated)
					{
						PMAccountGroup accountGroup = PMAccountGroup.PK.Find(this, balance.AccountGroupID);
						if (accountGroup != null && accountGroup.Type == GL.AccountType.Income)
						{
							PMRevenueBudget budget = new PMRevenueBudget();
							budget.ProjectID = Project.Current.ContractID;
							budget.ProjectTaskID = balance.TaskID;
							budget.AccountGroupID = balance.AccountGroupID;
							budget.InventoryID = balance.InventoryID;
							budget.CostCodeID = balance.CostCodeID;
							budget = RevenueBudget.Insert(budget);

							budget.Qty = balance.Quantity;
							budget.CuryAmount = balance.Amount;
							RevenueBudget.Update(budget);
						}
					}
				}

				this.Save.Press();
			}
		}

		public PXAction<PMProject> hold;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Hold")]
		protected virtual IEnumerable Hold(PXAdapter adapter) => adapter.Get();

		public PXAction<PMProject> activate;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Activate Project")]
		protected virtual IEnumerable Activate(PXAdapter adapter) => adapter.Get();

		public PXAction<PMProject> lockBudget;
		[PXUIField(DisplayName = "Lock Budget", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton]
		protected virtual IEnumerable LockBudget(PXAdapter adapter)
		{
			if (Project.Current != null)
			{
				Project.Current.BudgetFinalized = true;
				Project.Update(Project.Current);

				this.Save.Press();
			}
			return adapter.Get();
		}

		public PXAction<PMProject> unlockBudget;
		[PXUIField(DisplayName = "Unlock Budget", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton]
		protected virtual IEnumerable UnlockBudget(PXAdapter adapter)
		{

			if (Project.Current != null)
			{
				Project.Current.BudgetFinalized = false;
				Project.Update(Project.Current);

				this.Save.Press();
			}
			return adapter.Get();
		}

		public PXAction<PMProject> lockCommitments;
		[PXUIField(DisplayName = "Lock Commitments", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton]
		protected virtual IEnumerable LockCommitments(PXAdapter adapter)
		{
			if (Project.Current != null)
			{
				Project.Current.LockCommitments = true;
				Project.Update(Project.Current);

				this.Save.Press();
			}
			return adapter.Get();
		}

		public PXAction<PMProject> unlockCommitments;
		[PXUIField(DisplayName = "Unlock Commitments", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton]
		protected virtual IEnumerable UnlockCommitments(PXAdapter adapter)
		{

			if (Project.Current != null)
			{
				Project.Current.LockCommitments = false;
				Project.Update(Project.Current);

				this.Save.Press();
			}
			return adapter.Get();
		}

		

		public PXAction<PMProject> viewCostTransactions;

		[PXUIField(DisplayName = Messages.ViewTransactions, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.Inquiry)]
		public IEnumerable ViewCostTransactions(PXAdapter adapter)
		{
			if (CostBudget.Current != null)
			{
				TransactionInquiry target = PXGraph.CreateInstance<TransactionInquiry>();
				target.Filter.Insert(new TransactionInquiry.TranFilter());
				target.Filter.Current.ProjectID = CostBudget.Current.ProjectID;
				target.Filter.Current.AccountGroupID = CostFilter.Current?.GroupByTask == true ? null : CostBudget.Current.AccountGroupID;
				target.Filter.Current.ProjectTaskID = CostBudget.Current.TaskID;
				target.Filter.Current.IncludeUnreleased = false;
				if (!PXAccess.FeatureInstalled<FeaturesSet.costCodes>())
				{
					if (CostBudget.Current.InventoryID != null && CostBudget.Current.InventoryID != PMInventorySelectorAttribute.EmptyInventoryID)
						target.Filter.Current.InventoryID = CostFilter.Current?.GroupByTask == true ? null : CostBudget.Current.InventoryID;
				}
				else
				{
					if (CostBudget.Current.CostCodeID != null && CostBudget.Current.CostCodeID != CostCodeAttribute.DefaultCostCode) 
						target.Filter.Current.CostCode = CostFilter.Current?.GroupByTask == true ? null : CostBudget.Current.CostCodeID;
				}
				throw new PXPopupRedirectException(target, Messages.TransactionInquiry + " - " + Messages.ViewTransactions, true);
			}
			return adapter.Get();
		}

		public PXAction<PMProject> viewRevenueTransactions;

		[PXUIField(DisplayName = Messages.ViewTransactions, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.Inquiry)]
		public IEnumerable ViewRevenueTransactions(PXAdapter adapter)
		{
			if (RevenueBudget.Current != null)
			{
				TransactionInquiry target = PXGraph.CreateInstance<TransactionInquiry>();
				target.Filter.Insert(new TransactionInquiry.TranFilter());
				target.Filter.Current.ProjectID = RevenueBudget.Current.ProjectID;
				target.Filter.Current.AccountGroupID = RevenueFilter.Current?.GroupByTask == true ? null : RevenueBudget.Current.AccountGroupID;
				target.Filter.Current.ProjectTaskID = RevenueBudget.Current.TaskID;

					if ((Project.Current.BudgetLevel == BudgetLevels.Item || Project.Current.BudgetLevel == BudgetLevels.Detail) && RevenueBudget.Current.InventoryID != null && RevenueBudget.Current.InventoryID != PMInventorySelectorAttribute.EmptyInventoryID)
						target.Filter.Current.InventoryID = RevenueFilter.Current?.GroupByTask == true ? null : RevenueBudget.Current.InventoryID;
				
				if (PXAccess.FeatureInstalled<FeaturesSet.costCodes>())
				{
					if (Project.Current.BudgetLevel == BudgetLevels.CostCode && RevenueBudget.Current.CostCodeID != null && RevenueBudget.Current.CostCodeID != CostCodeAttribute.DefaultCostCode)
						target.Filter.Current.CostCode = RevenueFilter.Current?.GroupByTask == true ? null : RevenueBudget.Current.CostCodeID;
				}

				throw new PXPopupRedirectException(target, Messages.TransactionInquiry + " - " + Messages.ViewTransactions, true);
			}
			return adapter.Get();
		}

		public PXAction<PMProject> updateRetainage;
		[PXUIField(DisplayName = "Update Retainage", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton(DisplayOnMainToolbar = false, VisibleOnProcessingResults = false)]
		public virtual IEnumerable UpdateRetainage(PXAdapter adapter)
		{
			if (Project.Current != null)
			{
				bool hasDiscrepency = false;

				foreach (PMRevenueBudget budget in RevenueBudget.Select())
				{
					if (budget.RetainagePct != Project.Current.RetainagePct)
					{
						hasDiscrepency = true;
					}
				}

				if (hasDiscrepency)
				{
					if (Project.Current.RetainageMode == RetainageModes.Contract)
					{
						SyncRetainage();
					}
					else
					{
						WebDialogResult result = Project.Ask(Messages.RetaiangeChangedDialogHeader, Messages.RetaiangeChangedDialogQuestion, MessageButtons.YesNo, MessageIcon.Question);
						if (result == WebDialogResult.Yes)
						{
							SyncRetainage();
						}
					}
				}
			}
			return adapter.Get();
		}

		public virtual void SyncRetainage()
		{
			List<PMRevenueBudget> budgetToUpdate = new List<PMRevenueBudget>();

			foreach (PMRevenueBudget budget in RevenueBudget.Select())
			{
				if (budget.RetainagePct != Project.Current.RetainagePct)
				{
					budgetToUpdate.Add(budget);
				}
			}

			if (budgetToUpdate.Count > 0)
			{
				foreach (PMRevenueBudget budget in budgetToUpdate)
				{
					budget.RetainagePct = Project.Current.RetainagePct;
					RevenueBudget.Update(budget);
				}
			}
		}

		public PXAction<PMProject> viewReleaseRetainage;
		[PXUIField(DisplayName = Messages.ReleaseRetainage, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton()]
		public IEnumerable ViewReleaseRetainage(PXAdapter adapter)
		{
			if (Project.Current != null)
			{
				ARRetainageRelease target = PXGraph.CreateInstance<ARRetainageRelease>();
				target.Filter.Insert(new ARRetainageFilter());
				target.Filter.Current.ProjectID = Project.Current.ContractID;
				target.Filter.Current.CustomerID = Project.Current.CustomerID;
				target.Filter.Current.ShowBillsWithOpenBalance = true;

				throw new PXRedirectRequiredException(target, Messages.ReleaseRetainage, true);
			}
			return adapter.Get();
		}

		public PXAction<PMProject> copyProject;
		[PXUIField(DisplayName = "Copy Project", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		protected virtual IEnumerable CopyProject(PXAdapter adapter)
		{
			if (Project.Current != null)
			{
				this.Save.Press();

				IsCopyPaste = true;
				try
				{
					Copy(Project.Current);
				}
				finally
				{
					IsCopyPaste = false;
				}
			}
			return adapter.Get();
		}

		public PXAction<PMProject> createPurchaseOrder;
		[PXUIField(DisplayName = Messages.CreatePurchaseOrder, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable CreatePurchaseOrder(PXAdapter adapter)
		{
			return CreatePOOrderBase<PO.POOrderEntry>(adapter, Messages.CreatePurchaseOrder);
		}

		public virtual IEnumerable CreatePOOrderBase<TGraph>(PXAdapter adapter, string windowHeader)
			where TGraph: PO.POOrderEntry, new()
		{
			var graph = CreateInstance<TGraph>();
			if (PXAccess.FeatureInstalled<FeaturesSet.retainage>())
			{
				AP.APSetup apsetup = PXSelect<AP.APSetup>.Select(this);
				if (apsetup != null && apsetup.RequireSingleProjectPerDocument == true)
				{
					var porder = graph.Document.Insert();
					graph.Document.Cache.SetValueExt<PO.POOrder.projectID>(porder, Project.Current.ContractID);
				}
			}
			throw new PXRedirectRequiredException(graph, true, windowHeader) { Mode = PXBaseRedirectException.WindowMode.Same };
		}


		public PXAction<PMProject> ViewAddressOnMap;
		[PXUIField(DisplayName = PX.Objects.CR.Messages.ViewOnMap)]
		[PXButton(DisplayOnMainToolbar = false, VisibleOnProcessingResults = false)]
		public virtual void viewAddressOnMap()
		{
			PMSiteAddress address = Site_Address.Select();
			new MapService(this).viewAddressOnMap(address);
		}

		public PXAction<PMProject> complete;
		[PXButton, PXUIField(DisplayName = "Complete Project")]
		public virtual IEnumerable Complete(PXAdapter adapter)
		{
			if (Project.Current != null)
			{
				//Project can only be completed if all task are completed.
				PXResultset<PMTask> res = PXSelect<PMTask, Where<PMTask.projectID, Equal<Current<PMProject.contractID>>, And<PMTask.isCompleted, Equal<False>, And<PMTask.isCancelled, Equal<False>>>>>.Select(this);
				if (res.Count > 0)
				{
					Project.Cache.RaiseExceptionHandling<PMProject.status>(Project.Current, Project.Current.Status, new PXSetPropertyException<PMProject.status>(Messages.UncompletedTasksExist, PXErrorLevel.Error, res.Count));
					throw new PXException(Messages.UncompletedTasksExist, res.Count);
				}
				Project.Current.IsCompleted = true;
				Project.Current.ExpireDate = Accessinfo.BusinessDate;
				Project.Update(Project.Current);
			}
			return adapter.Get();
		}

		#endregion

		public ProjectEntry()
		{
			if (Setup.Current == null)
			{
				throw new PXException(Messages.SetupNotConfigured);
			}

			this.CopyPaste.SetVisible(false);
			Invoices.Cache.AllowDelete = false;
			Invoices.Cache.AllowInsert = false;
			Invoices.Cache.AllowUpdate = false;

			if (Project.Current != null && Project.Current.NonProject == true)
			{
				PXUIFieldAttribute.SetReadOnly(Project.Cache, null, true);
			}

			bool changeOrderFeature = PXAccess.FeatureInstalled<FeaturesSet.changeOrder>();
			lockCommitments.SetVisible(Setup.Current.CostCommitmentTracking == true && changeOrderFeature == true);
			unlockCommitments.SetVisible(Setup.Current.CostCommitmentTracking == true && changeOrderFeature == true);
			createChangeOrder.SetVisible(changeOrderFeature);

			viewCostCommitments.SetVisible(Setup.Current.CostCommitmentTracking == true);
			viewCommitments.SetVisible(Setup.Current.CostCommitmentTracking == true);
			
			bool projectMultiCurrency = PXAccess.FeatureInstalled<FeaturesSet.projectMultiCurrency>();
			currencyRates.SetVisible(projectMultiCurrency);
			setCurrencyRates.SetVisible(projectMultiCurrency);

			forecast.SetVisible(PXAccess.FeatureInstalled<FeaturesSet.budgetForecast>());
			viewReleaseRetainage.SetVisible(PXAccess.FeatureInstalled<FeaturesSet.retainage>());

			Activities.GetNewEmailAddress =
					() =>
					{
						PMProject current = Project.Current;
						if (current != null)
						{
							Contact customerContact = PXSelectJoin<Contact, InnerJoin<BAccount, On<BAccount.defContactID, Equal<Contact.contactID>>>, Where<BAccount.bAccountID, Equal<Required<BAccount.bAccountID>>>>.Select(this, current.CustomerID);

							if (customerContact != null && !string.IsNullOrWhiteSpace(customerContact.EMail))
								return PXDBEmailAttribute.FormatAddressesWithSingleDisplayName(customerContact.EMail, customerContact.DisplayName);
						}
						return String.Empty;
					};

			if (!new ProjectSettingsManager().CalculateProjectSpecificTaxes)
				PXDefaultAttribute.SetPersistingCheck<PMSiteAddress.countryID>(Site_Address.Cache, null, PXPersistingCheck.Nothing);
		}

		private IFinPeriodRepository finPeriodsRepo;
		public virtual IFinPeriodRepository FinPeriodRepository
		{
			get
			{
				if (finPeriodsRepo == null)
				{
					finPeriodsRepo = new FinPeriodRepository(this);
				}

				return finPeriodsRepo;
			}
		}

		private void BeforeCommitHandler(PXGraph e)
		{
			var check1 = _licenseLimits.GetCheckerDelegate<PMProject>(new TableQuery(TransactionTypes.LinesPerMasterRecord, typeof(PMTask), (graph) =>
			{
				return new PXDataFieldValue[]
				{
							new PXDataFieldValue<PMTask.projectID>(((ProjectEntry)graph).Project.Current?.ContractID)
				};
			}));
			
			try
			{
				check1.Invoke(e);
			}
			catch (PXException)
				{
				throw new PXException(Messages.LicenseTasks);
				}

			var revenueBudgetCheck = _licenseLimits.GetCheckerDelegate<PMProject>(new TableQuery(TransactionTypes.LinesPerMasterRecord, typeof(PMBudget), (graph) =>
			{
				return new PXDataFieldValue[]
				{
					new PXDataFieldValue<PMBudget.projectID>(((ProjectEntry)graph).Project.Current?.ContractID),
					new PXDataFieldValue<PMBudget.type>(PXDbType.Char, AccountType.Income)
				};
			}));

			try
			{
				revenueBudgetCheck.Invoke(e);
			}
			catch (PXException)
			{
				throw new PXException(Messages.LicenseRevenueBudget);
			}

			var costBudgetCheck = _licenseLimits.GetCheckerDelegate<PMProject>(new TableQuery(TransactionTypes.LinesPerMasterRecord, typeof(PMBudget), (graph) =>
			{
				return new PXDataFieldValue[]
				{
					new PXDataFieldValue<PMBudget.projectID>(((ProjectEntry)graph).Project.Current?.ContractID),
					new PXDataFieldValue<PMBudget.type>(PXDbType.Char, AccountType.Expense)
				};
			}));

			try
			{
				costBudgetCheck.Invoke(e);
			}
			catch (PXException)
			{
				throw new PXException(Messages.LicenseCostBudget);
			}
			}

		void IGraphWithInitialization.Initialize()
		{
			if (_licenseLimits != null)
			{
				OnBeforeCommit += BeforeCommitHandler;
			}
		}

		#region Event Handlers

		protected virtual void _(Events.RowSelected<SelectedTask> e)
		{
			if (e.Row != null)
			{
				bool found = false;
				foreach (PMTask task in Tasks.Select())
				{
					if (string.IsNullOrEmpty(task.TaskCD))
						continue;

					if (string.Equals(task.TaskCD.Trim(), e.Row.TaskCD.Trim(), StringComparison.InvariantCultureIgnoreCase))
					{
						found = true;
						break;
					}
				}

				PXUIFieldAttribute.SetWarning<SelectedTask.taskCD>(e.Cache, e.Row, found ? Messages.TaskAlreadyExists : null);
				PXUIFieldAttribute.SetEnabled(e.Cache, e.Row, !found);
			}
		}

		protected virtual void _(Events.RowPersisting<SelectedTask> e)
		{
			e.Cancel = true;
		}

		#region Task
		protected virtual void _(Events.RowSelected<PMTask> e)
		{
			if (e.Row != null)
			{
				PXUIFieldAttribute.SetEnabled<PMTask.locationID>(e.Cache, e.Row, e.Row.Status == ProjectTaskStatus.Planned);
				PXUIFieldAttribute.SetEnabled<PMTask.billingOption>(e.Cache, e.Row, e.Row.Status == ProjectTaskStatus.Planned);
				PXUIFieldAttribute.SetEnabled<PMTask.completedPercent>(e.Cache, e.Row, e.Row.Status != ProjectTaskStatus.Planned);
				PXUIFieldAttribute.SetEnabled<PMTask.plannedStartDate>(e.Cache, e.Row, e.Row.Status == ProjectTaskStatus.Planned);
				PXUIFieldAttribute.SetEnabled<PMTask.plannedEndDate>(e.Cache, e.Row, e.Row.Status == ProjectTaskStatus.Planned);
			}
		}

		protected virtual void _(Events.RowUpdated<PMTask> e)
		{
			if (e.Row != null)
			{
				if (this.IsCopyPasteContext)
				{
					e.Row.Status = PM.ProjectStatus.Planned;
					e.Row.IsActive = false;
					e.Row.IsCompleted = false;
					e.Row.IsCancelled = false;
					e.Row.StartDate = null;
					e.Row.EndDate = null;
					e.Row.CompletedPercent = 0;
				}

				if (this.IsMobile && string.IsNullOrEmpty(e.Row.TaskCD))
				{
					PXUIFieldAttribute.SetError<PMTask.taskCD>(e.Cache, e.Row, Messages.TaskIdEmptyError);
				}
			}
		}

		protected virtual void _(Events.RowDeleting<PMTask> e)
		{
			if (e.Row == null)
				return;

			ValidateAndRaiseErrorTaskCanBeDeleted(e.Row);
		}

		protected virtual void ValidateAndRaiseErrorTaskCanBeDeleted(PMTask row)
		{
			if (row.IsActive == true && row.IsCancelled == false)
			{
				throw new PXException(Messages.OnlyPlannedCanbeDeleted);
			}

			//validate that all child records can be deleted:

			PMTran tran = PXSelect<PMTran, Where<PMTran.projectID, Equal<Required<PMTask.projectID>>, And<PMTran.taskID, Equal<Required<PMTask.taskID>>>>>.SelectWindowed(this, 0, 1, row.ProjectID, row.TaskID);
			if (tran != null)
			{
				throw new PXException(Messages.HasTranData);
			}

			PMTimeActivity activity = PXSelect<PMTimeActivity, Where<PMTimeActivity.projectID, Equal<Required<PMTask.projectID>>, And<PMTimeActivity.projectTaskID, Equal<Required<PMTask.taskID>>>>>.SelectWindowed(this, 0, 1, row.ProjectID, row.TaskID);
			if (activity != null)
			{
				throw new PXException(Messages.HasActivityData);
			}

			EP.EPTimeCardItem timeCardItem = PXSelect<EP.EPTimeCardItem, Where<EP.EPTimeCardItem.projectID, Equal<Required<PMTask.projectID>>, And<EP.EPTimeCardItem.taskID, Equal<Required<PMTask.taskID>>>>>.SelectWindowed(this, 0, 1, row.ProjectID, row.TaskID);
			if (timeCardItem != null)
			{
				throw new PXException(Messages.HasTimeCardItemData);
			}
		}

		protected virtual void _(Events.FieldVerifying<PMTask, PMTask.plannedStartDate> e)
		{
			if (e.Row != null)
			{
				if (e.NewValue != null && Project.Current != null && Project.Current.StartDate != null)
				{
					DateTime date = (DateTime)e.NewValue;

					if (date.Date < Project.Current.StartDate.Value.Date)
					{
						e.Cache.RaiseExceptionHandling<PMTask.plannedStartDate>(e.Row, e.NewValue, new PXSetPropertyException<PMTask.plannedStartDate>(Warnings.StartDateOverlow, PXErrorLevel.Warning));
					}

					if (e.Row.PlannedEndDate != null && date > e.Row.PlannedEndDate)
					{
						e.Cache.RaiseExceptionHandling<PMTask.plannedStartDate>(e.Row, e.NewValue, new PXSetPropertyException<PMTask.plannedStartDate>(Messages.StartEndDateInvalid, PXErrorLevel.Error));
					}
				}
			}
		}

		protected virtual void _(Events.FieldVerifying<PMTask, PMTask.plannedEndDate> e)
		{
			if (e.Row != null)
			{
				if (e.NewValue != null && Project.Current != null && Project.Current.ExpireDate != null)
				{
					DateTime date = (DateTime)e.NewValue;

					if (date.Date > Project.Current.ExpireDate.Value.Date)
					{
						e.Cache.RaiseExceptionHandling<PMTask.plannedEndDate>(e.Row, e.NewValue, new PXSetPropertyException<PMTask.plannedEndDate>(Warnings.EndDateOverlow, PXErrorLevel.Warning));
					}

					if (e.Row.PlannedStartDate != null && date < e.Row.PlannedStartDate)
					{
						e.Cache.RaiseExceptionHandling<PMTask.plannedEndDate>(e.Row, e.NewValue, new PXSetPropertyException<PMTask.plannedEndDate>(Messages.StartEndDateInvalid, PXErrorLevel.Error));
					}
				}
			}
		}

		protected virtual void _(Events.FieldVerifying<PMTask, PMTask.status> e)
		{
			if ((string)e.NewValue == ProjectTaskStatus.Active)
			{
				HashSet<string> requiredAttributes = new HashSet<string>();
				var select = new PXSelect<CSAttributeGroup,
						Where<CSAttributeGroup.entityClassID, Equal<GroupTypes.TaskType>,
						And<CSAttributeGroup.required, Equal<True>>>>(this);
				foreach(CSAttributeGroup group in select.Select())
				{
					requiredAttributes.Add(group.AttributeID);
				}
				
				if (requiredAttributes.Count > 0)
				{
					if (Tasks.Cache.GetStatus(e.Row) == PXEntryStatus.Inserted )
					{
						throw new PXSetPropertyException<PMTask.status>(Messages.TaskReferencesRequiredAttributes);
					}
					else
					{
						var selectAnswers = new PXSelect<CSAnswers, Where<CSAnswers.refNoteID, Equal<Required<PMTask.noteID>>>>(this);

						foreach(CSAnswers ans in selectAnswers.Select(e.Row.NoteID))
						{
							if (!string.IsNullOrEmpty(ans.Value))
							{
								requiredAttributes.Remove(ans.AttributeID);
							}
						}

						if (requiredAttributes.Count > 0)
							throw new PXSetPropertyException<PMTask.status>(Messages.TaskReferencesRequiredAttributes);
					}
				}
			}
		}

		protected virtual void _(Events.FieldUpdated<PMTask, PMTask.status> e)
		{
			//Workflow is handled here since AU cannot be used for detail record.

			if (e.Row != null)
			{
				switch (e.Row.Status)
				{
					case ProjectTaskStatus.Active:
						e.Row.IsActive = true;
						e.Row.IsCompleted = false;
						e.Row.IsCancelled = false;
						if (e.Row.StartDate == null)
							e.Row.StartDate = Accessinfo.BusinessDate;
						break;
					case ProjectTaskStatus.Canceled:
						e.Row.IsActive = false;
						e.Row.IsCompleted = false;
						e.Row.IsCancelled = true;
						break;
					case ProjectTaskStatus.Completed:
						e.Row.IsActive = true;
						e.Row.IsCompleted = true;
						e.Row.IsCancelled = false;
						e.Row.EndDate = Accessinfo.BusinessDate;
						e.Row.CompletedPercent = Math.Max(100, e.Row.CompletedPercent.GetValueOrDefault());
						break;
					case ProjectTaskStatus.Planned:
						e.Row.IsActive = false;
						e.Row.IsCompleted = false;
						e.Row.IsCancelled = false;
						break;
				}
			}
		}

		protected virtual void _(Events.FieldUpdated<PMTask, PMTask.isDefault> e)
		{
			if (e.Row.IsDefault == true)
			{
				bool requestRefresh = false;
				foreach(PMTask task in Tasks.Select())
				{
					if (task.IsDefault == true && task.TaskID != e.Row.TaskID)
					{
						Tasks.Cache.SetValue<PMTask.isDefault>(task, false);
						Tasks.Cache.SmartSetStatus(task, PXEntryStatus.Updated);

						requestRefresh = true;
					}
				}

				if (requestRefresh)
				{
					Tasks.View.RequestRefresh();
				}
			}
		}

		Dictionary<int, List<PMCostBudget>> costBudgetsByRevenueTaskID = new Dictionary<int, List<PMCostBudget>>();
		Dictionary<int?, int?> persistedTask = new Dictionary<int?, int?>();
		int? negativeKey = null;
		protected virtual void _(Events.RowPersisting<PMTask> e)
		{
			if (e.Operation != PXDBOperation.Delete && e.Row.Status == ProjectTaskStatus.Active && e.Row.IsActive != true)
				throw new PXException(Messages.TaskCannotBeSaved, e.Row.TaskCD);

			if (e.Operation == PXDBOperation.Insert)
			{
				negativeKey = e.Row.TaskID;
			}
		}

		protected virtual void _(Events.RowPersisted<PMTask> e)
		{
			if (e.Operation == PXDBOperation.Insert && e.TranStatus == PXTranStatus.Open && negativeKey != null)
			{
				int? newKey = e.Row.TaskID;

				List<PMCostBudget> taskCostBudgets;
				if (negativeKey != null && costBudgetsByRevenueTaskID.TryGetValue(negativeKey.Value, out taskCostBudgets))
				{
					foreach (PMCostBudget budget in taskCostBudgets)
					{
						CostBudget.Cache.SetValue<PMCostBudget.revenueTaskID>(budget, newKey);
						if (!persistedTask.ContainsKey(newKey))
							persistedTask.Add(newKey, negativeKey);
					}
				}

				negativeKey = null;
			}

			if (e.Operation == PXDBOperation.Insert && e.TranStatus == PXTranStatus.Aborted)
			{
				int? negativeTaskID;
				if (persistedTask.TryGetValue(e.Row.TaskID, out negativeTaskID))
					{
					List<PMCostBudget> taskCostBudgets;
					if (negativeKey != null && costBudgetsByRevenueTaskID.TryGetValue(negativeTaskID.Value, out taskCostBudgets))
				{
						foreach (PMCostBudget budget in taskCostBudgets)
					{
							CostBudget.Cache.SetValue<PMCostBudget.revenueTaskID>(budget, negativeTaskID);
						}
					}
				}
			}
		}

		#endregion

		#region Project

		protected virtual void _(Events.RowSelecting<PMProject> e)
		{
			if (e.Row != null && e.Row.CuryCapAmount == null)
			{
				using (new PXConnectionScope())
				{
					e.Row.CuryCapAmount = CalculateCapAmount(e.Row, (PMProjectRevenueTotal)ProjectRevenueTotals.View.SelectSingleBound(new object[] { e.Row }));
				}
			}
		}

		protected virtual void _(Events.RowSelected<PMProject> e)
		{
			//Stopwatch sw = new Stopwatch();
			//sw.Start();

			bool changeOrderAutoNumbering = true;
			PXResult<Numbering> changeOrderNumbering = (PXResult<Numbering>)SelectFrom<Numbering>.
				Where<Numbering.numberingID.IsEqual<@P.AsString>>.View.Select(this, Setup.Current.ChangeOrderNumbering).FirstOrDefault();
			if (changeOrderNumbering != null && ((Numbering)changeOrderNumbering).UserNumbering == true)
			{
				changeOrderAutoNumbering = false;
			}
			createChangeOrder.SetVisible(PXAccess.FeatureInstalled<FeaturesSet.changeOrder>() && changeOrderAutoNumbering);

			if (e.Row != null)
			{
				PXUIFieldAttribute.SetEnabled<PMProject.visibleInGL>(e.Cache, e.Row, Setup.Current.VisibleInGL == true);
				PXUIFieldAttribute.SetEnabled<PMProject.visibleInAP>(e.Cache, e.Row, Setup.Current.VisibleInAP == true);
				PXUIFieldAttribute.SetEnabled<PMProject.visibleInAR>(e.Cache, e.Row, Setup.Current.VisibleInAR == true);
				PXUIFieldAttribute.SetEnabled<PMProject.visibleInSO>(e.Cache, e.Row, Setup.Current.VisibleInSO == true);
				PXUIFieldAttribute.SetEnabled<PMProject.visibleInPO>(e.Cache, e.Row, Setup.Current.VisibleInPO == true);
				PXUIFieldAttribute.SetEnabled<PMProject.visibleInTA>(e.Cache, e.Row, Setup.Current.VisibleInTA == true);
				PXUIFieldAttribute.SetEnabled<PMProject.visibleInEA>(e.Cache, e.Row, Setup.Current.VisibleInEA == true);
				PXUIFieldAttribute.SetEnabled<PMProject.visibleInIN>(e.Cache, e.Row, Setup.Current.VisibleInIN == true);
				PXUIFieldAttribute.SetEnabled<PMProject.visibleInCA>(e.Cache, e.Row, Setup.Current.VisibleInCA == true);
				PXUIFieldAttribute.SetEnabled<PMProject.visibleInCR>(e.Cache, e.Row, Setup.Current.VisibleInCR == true);
				PXUIFieldAttribute.SetEnabled<PMProject.templateID>(e.Cache, e.Row, e.Row.TemplateID == null && e.Cache.GetStatus(e.Row) == PXEntryStatus.Inserted);
				PXUIFieldAttribute.SetEnabled<PMProject.createProforma>(e.Cache, e.Row, Project.Current.CustomerID != null);
				PXUIFieldAttribute.SetEnabled<PMProject.automaticReleaseAR>(e.Cache, e.Row, Project.Current.CustomerID != null);
								
				Tasks.Cache.AllowInsert = e.Row.IsCompleted != true && e.Row.IsCancelled != true;
				Tasks.Cache.AllowUpdate = e.Row.IsCompleted != true && e.Row.IsCancelled != true;
				Tasks.Cache.AllowDelete = e.Row.IsCompleted != true && e.Row.IsCancelled != true;

				ContractRates.Cache.AllowInsert = e.Row.IsCompleted != true && e.Row.IsCancelled != true;
				ContractRates.Cache.AllowUpdate = e.Row.IsCompleted != true && e.Row.IsCancelled != true;
				ContractRates.Cache.AllowDelete = e.Row.IsCompleted != true && e.Row.IsCancelled != true;

				EmployeeContract.Cache.AllowInsert = e.Row.IsCompleted != true && e.Row.IsCancelled != true;
				EmployeeContract.Cache.AllowUpdate = e.Row.IsCompleted != true && e.Row.IsCancelled != true;
				EmployeeContract.Cache.AllowDelete = e.Row.IsCompleted != true && e.Row.IsCancelled != true;

				EquipmentRates.Cache.AllowInsert = e.Row.IsCompleted != true && e.Row.IsCancelled != true;
				EquipmentRates.Cache.AllowUpdate = e.Row.IsCompleted != true && e.Row.IsCancelled != true;
				EquipmentRates.Cache.AllowDelete = e.Row.IsCompleted != true && e.Row.IsCancelled != true;
				
				CostBudget.Cache.AllowInsert = CostBudgetIsEditable();
				CostBudget.Cache.AllowUpdate = CostBudgetIsEditable();
				CostBudget.Cache.AllowDelete = CostBudgetIsEditable();

				RevenueBudget.Cache.AllowInsert = RevenueBudgetIsEditable();
				RevenueBudget.Cache.AllowUpdate = RevenueBudgetIsEditable();
				RevenueBudget.Cache.AllowDelete = RevenueBudgetIsEditable();

				RetainageSteps.AllowSelect = e.Row.SteppedRetainage == true;

				PurchaseOrders.Cache.AllowSelect = Setup.Current.CostCommitmentTracking == true;
								
				lockCommitments.SetEnabled(e.Row.LockCommitments != true);
				unlockCommitments.SetEnabled(e.Row.LockCommitments == true);
				lockBudget.SetEnabled(e.Row.BudgetFinalized != true);
				unlockBudget.SetEnabled(e.Row.BudgetFinalized == true);
				createChangeOrder.SetEnabled(ChangeOrderVisible());
				laborCostRates.SetEnabled(e.Cache.GetStatus(e.Row) != PXEntryStatus.Inserted);

				bool payByLine = PXAccess.FeatureInstalled<FeaturesSet.paymentsByLines>();
				PXUIFieldAttribute.SetVisible<PMProject.retainageMode>(e.Cache, e.Row, payByLine);
				PXUIFieldAttribute.SetVisible<PMProject.retainageMaxPct>(e.Cache, e.Row, payByLine && e.Row.RetainageMode == RetainageModes.Contract);
				PXUIFieldAttribute.SetVisible<PMProject.curyCapAmount>(e.Cache, e.Row, payByLine && e.Row.RetainageMode == RetainageModes.Contract);
				PXUIFieldAttribute.SetVisible<PMProjectRevenueTotal.curyAmount>(ProjectRevenueTotals.Cache, null, PXAccess.FeatureInstalled<FeaturesSet.retainage>() && e.Row.IncludeCO != true);
				PXUIFieldAttribute.SetVisible<PMProjectRevenueTotal.curyRevisedAmount>(ProjectRevenueTotals.Cache, null, PXAccess.FeatureInstalled<FeaturesSet.retainage>() && e.Row.IncludeCO == true);
				PXUIFieldAttribute.SetVisible<PMProjectRevenueTotal.contractCompletedPct>(ProjectRevenueTotals.Cache, null, PXAccess.FeatureInstalled<FeaturesSet.retainage>() && e.Row.IncludeCO != true);
				PXUIFieldAttribute.SetVisible<PMProjectRevenueTotal.contractCompletedWithCOPct>(ProjectRevenueTotals.Cache, null, PXAccess.FeatureInstalled<FeaturesSet.retainage>() && e.Row.IncludeCO == true);
				PXUIFieldAttribute.SetEnabled<PMProject.retainagePct>(e.Cache, e.Row, e.Row.SteppedRetainage != true);
				PXUIFieldAttribute.SetEnabled<PMProject.steppedRetainage>(e.Cache, e.Row, e.Row.RetainageMode != RetainageModes.Line);
				PXUIFieldAttribute.SetEnabled<PMProject.accountingMode>(e.Cache, e.Row, PXAccess.FeatureInstalled<FeaturesSet.materialManagement>());
				PXUIFieldAttribute.SetEnabled<PMProject.revenueTaxZoneID>(e.Cache, e.Row, Setup.Current.CalculateProjectSpecificTaxes == true);
				PXUIFieldAttribute.SetEnabled<PMProject.costTaxZoneID>(e.Cache, e.Row, Setup.Current.CalculateProjectSpecificTaxes == true);
				PXUIFieldAttribute.SetVisible<PMProject.rateTypeID>(e.Cache, e.Row, PXAccess.FeatureInstalled<FeaturesSet.multicurrency>() || PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>() || PXAccess.FeatureInstalled<FeaturesSet.projectMultiCurrency>());
				PXUIFieldAttribute.SetEnabled<PMProject.includeCO>(e.Cache, e.Row, e.Row.ChangeOrderWorkflow == true);

				PXUIFieldAttribute.SetVisible<PMCostBudget.curyCommittedAmount>(CostBudget.Cache, null, Setup.Current.CostCommitmentTracking == true);
				PXUIFieldAttribute.SetVisible<PMCostBudget.committedQty>(CostBudget.Cache, null, Setup.Current.CostCommitmentTracking == true && CostQuantityVisible());
				PXUIFieldAttribute.SetVisible<PMCostBudget.curyCommittedOrigAmount>(CostBudget.Cache, null, Setup.Current.CostCommitmentTracking == true);
				PXUIFieldAttribute.SetVisible<PMCostBudget.committedOrigQty>(CostBudget.Cache, null, Setup.Current.CostCommitmentTracking == true && CostQuantityVisible());
				PXUIFieldAttribute.SetVisible<PMCostBudget.curyCommittedCOAmount>(CostBudget.Cache, null, ChangeOrderVisible() && Setup.Current.CostCommitmentTracking == true);
				PXUIFieldAttribute.SetVisible<PMCostBudget.committedCOQty>(CostBudget.Cache, null, ChangeOrderVisible() && Setup.Current.CostCommitmentTracking == true && CostQuantityVisible());
				PXUIFieldAttribute.SetVisible<PMCostBudget.curyCommittedInvoicedAmount>(CostBudget.Cache, null, Setup.Current.CostCommitmentTracking == true);
				PXUIFieldAttribute.SetVisible<PMCostBudget.committedInvoicedQty>(CostBudget.Cache, null, Setup.Current.CostCommitmentTracking == true && CostQuantityVisible());
				PXUIFieldAttribute.SetVisible<PMCostBudget.curyCommittedOpenAmount>(CostBudget.Cache, null, Setup.Current.CostCommitmentTracking == true);
				PXUIFieldAttribute.SetVisible<PMCostBudget.committedOpenQty>(CostBudget.Cache, null, Setup.Current.CostCommitmentTracking == true && CostQuantityVisible());
				PXUIFieldAttribute.SetVisible<PMCostBudget.committedReceivedQty>(CostBudget.Cache, null, Setup.Current.CostCommitmentTracking == true && CostQuantityVisible());
				PXUIFieldAttribute.SetVisible<PMCostBudget.curyActualPlusOpenCommittedAmount>(CostBudget.Cache, null, Setup.Current.CostCommitmentTracking == true);
				PXUIFieldAttribute.SetVisible<PMCostBudget.curyVarianceAmount>(CostBudget.Cache, null, Setup.Current.CostCommitmentTracking == true);
				PXUIFieldAttribute.SetVisible<PMCostBudget.inventoryID>(CostBudget.Cache, null, (ProxyIsActive || e.Row.CostBudgetLevel == BudgetLevels.Item || e.Row.CostBudgetLevel == BudgetLevels.Detail) && CostBudgetIsEditable());
				PXUIFieldAttribute.SetVisible<PMCostBudget.costCodeID>(CostBudget.Cache, null, (ProxyIsActive || e.Row.CostBudgetLevel == BudgetLevels.CostCode || e.Row.CostBudgetLevel == BudgetLevels.Detail) && CostBudgetIsEditable());
				PXUIFieldAttribute.SetVisible<PMCostBudget.revenueInventoryID>(CostBudget.Cache, null, (ProxyIsActive || e.Row.CostBudgetLevel == BudgetLevels.Item || e.Row.CostBudgetLevel == BudgetLevels.Detail) && CostBudgetIsEditable());
				PXUIFieldAttribute.SetVisible<PMCostBudget.revenueTaskID>(CostBudget.Cache, null, CostBudgetIsEditable());
				PXUIFieldAttribute.SetVisible<PMCostBudget.curyUnitRate>(CostBudget.Cache, null, CostQuantityVisible());
				PXUIFieldAttribute.SetVisible<PMCostBudget.qty>(CostBudget.Cache, null, CostQuantityVisible());
				PXUIFieldAttribute.SetVisible<PMCostBudget.revisedQty>(CostBudget.Cache, null, CostQuantityVisible());
				PXUIFieldAttribute.SetVisible<PMCostBudget.actualQty>(CostBudget.Cache, null, CostQuantityVisible());
				PXUIFieldAttribute.SetVisible<PMCostBudget.uOM>(CostBudget.Cache, null, CostQuantityVisible());
				PXUIFieldAttribute.SetVisible<PMCostBudget.isProduction>(CostBudget.Cache, null, CostQuantityVisible());
				PXUIFieldAttribute.SetVisible<PMCostBudget.accountGroupID>(CostBudget.Cache, null, !IsCostGroupByTask());
				
				PXUIFieldAttribute.SetEnabled<PMCostBudget.curyUnitRate>(CostBudget.Cache, null, CostBudgetIsEditable() && BudgetEditable());
				PXUIFieldAttribute.SetEnabled<PMCostBudget.qty>(CostBudget.Cache, null,  CostBudgetIsEditable() && BudgetEditable());
				PXUIFieldAttribute.SetEnabled<PMCostBudget.curyAmount>(CostBudget.Cache, null, CostBudgetIsEditable() && BudgetEditable());
				PXUIFieldAttribute.SetEnabled<PMCostBudget.revisedQty>(CostBudget.Cache, null, CostBudgetIsEditable() && RevisedEditable());
				PXUIFieldAttribute.SetEnabled<PMCostBudget.curyRevisedAmount>(CostBudget.Cache, null, CostBudgetIsEditable() && RevisedEditable());
				PXUIFieldAttribute.SetVisible<PMCostBudget.draftChangeOrderQty>(CostBudget.Cache, null, ChangeOrderVisible() && CostQuantityVisible());
				PXUIFieldAttribute.SetVisible<PMCostBudget.curyDraftChangeOrderAmount>(CostBudget.Cache, null, ChangeOrderVisible());
				PXUIFieldAttribute.SetVisible<PMCostBudget.changeOrderQty>(CostBudget.Cache, null, ChangeOrderVisible() && CostQuantityVisible());
				PXUIFieldAttribute.SetVisible<PMCostBudget.curyChangeOrderAmount>(CostBudget.Cache, null, ChangeOrderVisible());
				PXUIFieldAttribute.SetVisible<PMCostBudget.curyLastCostAtCompletion>(CostBudget.Cache, null, ProductivityVisible());
				PXUIFieldAttribute.SetVisible<PMCostBudget.curyCostAtCompletion>(CostBudget.Cache, null, ProductivityVisible());
				PXUIFieldAttribute.SetVisible<PMCostBudget.curyLastCostToComplete>(CostBudget.Cache, null, ProductivityVisible());
				PXUIFieldAttribute.SetVisible<PMCostBudget.curyCostToComplete>(CostBudget.Cache, null, ProductivityVisible());
				PXUIFieldAttribute.SetVisible<PMCostBudget.lastPercentCompleted>(CostBudget.Cache, null, ProductivityVisible());
				PXUIFieldAttribute.SetVisible<PMCostBudget.percentCompleted>(CostBudget.Cache, null, ProductivityVisible());
				PXUIFieldAttribute.SetEnabled<PMCostBudget.curyCostAtCompletion>(CostBudget.Cache, null, ProductivityVisible());//to hide import from excel columns
				PXUIFieldAttribute.SetEnabled<PMCostBudget.curyCostToComplete>(CostBudget.Cache, null, ProductivityVisible());//to hide import from excel columns
				PXUIFieldAttribute.SetEnabled<PMCostBudget.percentCompleted>(CostBudget.Cache, null, ProductivityVisible());//to hide import from excel columns

				PXUIFieldAttribute.SetVisibility<PMCostBudget.revenueInventoryID>(CostBudget.Cache, null, e.Row.BudgetLevel == BudgetLevels.Item || e.Row.BudgetLevel == BudgetLevels.Detail ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMCostBudget.curyCommittedAmount>(CostBudget.Cache, null, Setup.Current.CostCommitmentTracking == true ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMCostBudget.committedQty>(CostBudget.Cache, null, Setup.Current.CostCommitmentTracking == true  ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMCostBudget.curyCommittedCOAmount>(CostBudget.Cache, null, ChangeOrderVisible() && Setup.Current.CostCommitmentTracking == true ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMCostBudget.committedCOQty>(CostBudget.Cache, null, ChangeOrderVisible() && Setup.Current.CostCommitmentTracking == true ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMCostBudget.curyCommittedInvoicedAmount>(CostBudget.Cache, null, Setup.Current.CostCommitmentTracking == true ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMCostBudget.committedInvoicedQty>(CostBudget.Cache, null, Setup.Current.CostCommitmentTracking == true  ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMCostBudget.curyCommittedOpenAmount>(CostBudget.Cache, null, Setup.Current.CostCommitmentTracking == true ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMCostBudget.committedOpenQty>(CostBudget.Cache, null, Setup.Current.CostCommitmentTracking == true  ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMCostBudget.committedReceivedQty>(CostBudget.Cache, null, Setup.Current.CostCommitmentTracking == true  ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMCostBudget.curyActualPlusOpenCommittedAmount>(CostBudget.Cache, null, Setup.Current.CostCommitmentTracking == true ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMCostBudget.curyVarianceAmount>(CostBudget.Cache, null, Setup.Current.CostCommitmentTracking == true ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMCostBudget.draftChangeOrderQty>(CostBudget.Cache, null, ChangeOrderVisible() ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMCostBudget.curyDraftChangeOrderAmount>(CostBudget.Cache, null, ChangeOrderVisible() ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMCostBudget.changeOrderQty>(CostBudget.Cache, null, ChangeOrderVisible()  ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMCostBudget.curyChangeOrderAmount>(CostBudget.Cache, null, ChangeOrderVisible() ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				
				PXUIFieldAttribute.SetVisibility<PMCostBudget.curyLastCostAtCompletion>(CostBudget.Cache, null, CostCodeAttribute.UseCostCode() ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMCostBudget.curyCostAtCompletion>(CostBudget.Cache, null, CostCodeAttribute.UseCostCode() ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMCostBudget.curyLastCostToComplete>(CostBudget.Cache, null, CostCodeAttribute.UseCostCode() ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMCostBudget.curyCostToComplete>(CostBudget.Cache, null, CostCodeAttribute.UseCostCode() ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMCostBudget.lastPercentCompleted>(CostBudget.Cache, null, CostCodeAttribute.UseCostCode() ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMCostBudget.percentCompleted>(CostBudget.Cache, null, CostCodeAttribute.UseCostCode() ? PXUIVisibility.Visible : PXUIVisibility.Invisible);

				PXUIFieldAttribute.SetVisible<PMRevenueBudget.curyCommittedAmount>(RevenueBudget.Cache, null, false);
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.committedQty>(RevenueBudget.Cache, null, false);
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.curyCommittedInvoicedAmount>(RevenueBudget.Cache, null, false);
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.committedInvoicedQty>(RevenueBudget.Cache, null, false);
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.curyCommittedOpenAmount>(RevenueBudget.Cache, null, false);
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.committedOpenQty>(RevenueBudget.Cache, null, false);
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.committedReceivedQty>(RevenueBudget.Cache, null, false);
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.curyActualPlusOpenCommittedAmount>(RevenueBudget.Cache, null, false);
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.curyVarianceAmount>(RevenueBudget.Cache, null, false);
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.inventoryID>(RevenueBudget.Cache, null, RevenueBudgetIsEditable() && (ProxyIsActive || e.Row.BudgetLevel == BudgetLevels.Item || e.Row.BudgetLevel == BudgetLevels.Detail));
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.costCodeID>(RevenueBudget.Cache, null, RevenueBudgetIsEditable() && (ProxyIsActive || e.Row.BudgetLevel == BudgetLevels.CostCode || e.Row.BudgetLevel == BudgetLevels.Detail));
				PXUIFieldAttribute.SetRequired<PMRevenueBudget.inventoryID>(RevenueBudget.Cache, e.Row.BudgetLevel == BudgetLevels.Item || e.Row.BudgetLevel == BudgetLevels.Detail);
				PXUIFieldAttribute.SetEnabled<PMRevenueBudget.curyUnitRate>(RevenueBudget.Cache, null, RevenueBudgetIsEditable() && BudgetEditable());
				PXUIFieldAttribute.SetEnabled<PMRevenueBudget.qty>(RevenueBudget.Cache, null, RevenueBudgetIsEditable() && BudgetEditable());
				PXUIFieldAttribute.SetEnabled<PMRevenueBudget.curyAmount>(RevenueBudget.Cache, null, RevenueBudgetIsEditable() && BudgetEditable());
				PXUIFieldAttribute.SetEnabled<PMRevenueBudget.revisedQty>(RevenueBudget.Cache, null, RevenueBudgetIsEditable() && RevisedEditable());
				PXUIFieldAttribute.SetEnabled<PMRevenueBudget.curyRevisedAmount>(RevenueBudget.Cache, null, RevenueBudgetIsEditable() && RevisedEditable());
				PXUIFieldAttribute.SetEnabled<PMRevenueBudget.completedPct>(RevenueBudget.Cache, null, Setup.Current.AutoCompleteRevenueBudget != true);
				PXUIFieldAttribute.SetEnabled<PMRevenueBudget.curyAmountToInvoice>(RevenueBudget.Cache, null, Setup.Current.AutoCompleteRevenueBudget != true);
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.draftChangeOrderQty>(RevenueBudget.Cache, null, ChangeOrderVisible() && RevenueQuantityVisible());
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.curyDraftChangeOrderAmount>(RevenueBudget.Cache, null, ChangeOrderVisible());
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.changeOrderQty>(RevenueBudget.Cache, null, ChangeOrderVisible() && RevenueQuantityVisible());
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.curyChangeOrderAmount>(RevenueBudget.Cache, null, ChangeOrderVisible());
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.curyPrepaymentAmount>(RevenueBudget.Cache, null, PrepaymentVisible());
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.curyPrepaymentInvoiced>(RevenueBudget.Cache, null, PrepaymentVisible());
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.curyPrepaymentAvailable>(RevenueBudget.Cache, null, PrepaymentVisible());
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.prepaymentPct>(RevenueBudget.Cache, null, PrepaymentVisible());
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.limitQty>(RevenueBudget.Cache, null, false && RevenueQuantityVisible());
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.maxQty>(RevenueBudget.Cache, null, false && RevenueQuantityVisible());
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.limitAmount>(RevenueBudget.Cache, null, LimitsVisible());
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.curyMaxAmount>(RevenueBudget.Cache, null, LimitsVisible());
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.curyUnitRate>(RevenueBudget.Cache, null, RevenueQuantityVisible());
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.qty>(RevenueBudget.Cache, null, RevenueQuantityVisible());
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.revisedQty>(RevenueBudget.Cache, null, RevenueQuantityVisible());
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.actualQty>(RevenueBudget.Cache, null, RevenueQuantityVisible());
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.uOM>(RevenueBudget.Cache, null, RevenueQuantityVisible());
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.isProduction>(RevenueBudget.Cache, null, RevenueQuantityVisible());
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.accountGroupID>(RevenueBudget.Cache, null, !IsRevenueGroupByTask());
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.completedPct>(RevenueBudget.Cache, null, RevenueQuantityVisible());
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.taxCategoryID>(RevenueBudget.Cache, null, RevenueQuantityVisible());
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.qtyToInvoice>(RevenueBudget.Cache, null, RevenueQuantityVisible());
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.invoicedQty>(RevenueBudget.Cache, null, RevenueQuantityVisible());

				PXUIFieldAttribute.SetVisibility<PMRevenueBudget.curyCommittedAmount>(RevenueBudget.Cache, null, PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMRevenueBudget.committedQty>(RevenueBudget.Cache, null, PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMRevenueBudget.curyCommittedInvoicedAmount>(RevenueBudget.Cache, null, PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMRevenueBudget.committedInvoicedQty>(RevenueBudget.Cache, null, PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMRevenueBudget.curyCommittedOpenAmount>(RevenueBudget.Cache, null, PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMRevenueBudget.committedOpenQty>(RevenueBudget.Cache, null, PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMRevenueBudget.committedReceivedQty>(RevenueBudget.Cache, null, PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMRevenueBudget.curyActualPlusOpenCommittedAmount>(RevenueBudget.Cache, null, PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMRevenueBudget.curyVarianceAmount>(RevenueBudget.Cache, null, PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMRevenueBudget.draftChangeOrderQty>(RevenueBudget.Cache, null, ChangeOrderVisible() ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMRevenueBudget.curyDraftChangeOrderAmount>(RevenueBudget.Cache, null, ChangeOrderVisible() ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMRevenueBudget.changeOrderQty>(RevenueBudget.Cache, null, ChangeOrderVisible() ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMRevenueBudget.curyChangeOrderAmount>(RevenueBudget.Cache, null, ChangeOrderVisible() ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMRevenueBudget.curyPrepaymentAmount>(RevenueBudget.Cache, null, PrepaymentVisible() ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMRevenueBudget.curyPrepaymentInvoiced>(RevenueBudget.Cache, null, PrepaymentVisible() ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMRevenueBudget.curyPrepaymentAvailable>(RevenueBudget.Cache, null, PrepaymentVisible() ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMRevenueBudget.prepaymentPct>(RevenueBudget.Cache, null, PrepaymentVisible() ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMRevenueBudget.limitQty>(RevenueBudget.Cache, null, false ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMRevenueBudget.maxQty>(RevenueBudget.Cache, null, false ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMRevenueBudget.limitAmount>(RevenueBudget.Cache, null, LimitsVisible() ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMRevenueBudget.curyMaxAmount>(RevenueBudget.Cache, null, LimitsVisible() ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.retainagePct>(RevenueBudget.Cache, null, e.Row.RetainageMode != RetainageModes.Contract && e.Row.SteppedRetainage != true);
				PXUIFieldAttribute.SetVisibility<PMRevenueBudget.retainagePct>(RevenueBudget.Cache, null, e.Row.RetainageMode != RetainageModes.Contract && e.Row.SteppedRetainage != true ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.curyDraftRetainedAmount>(RevenueBudget.Cache, null, payByLine);
				PXUIFieldAttribute.SetVisibility<PMRevenueBudget.curyDraftRetainedAmount>(RevenueBudget.Cache, null, payByLine ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.curyRetainedAmount>(RevenueBudget.Cache, null, payByLine);
				PXUIFieldAttribute.SetVisibility<PMRevenueBudget.curyRetainedAmount>(RevenueBudget.Cache, null, payByLine ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.curyTotalRetainedAmount>(RevenueBudget.Cache, null, payByLine);
				PXUIFieldAttribute.SetVisibility<PMRevenueBudget.curyTotalRetainedAmount>(RevenueBudget.Cache, null, payByLine ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.retainageMaxPct>(RevenueBudget.Cache, null, e.Row.RetainageMode == RetainageModes.Line);
				PXUIFieldAttribute.SetVisibility<PMRevenueBudget.retainageMaxPct>(RevenueBudget.Cache, null, e.Row.RetainageMode == RetainageModes.Line ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.curyCapAmount>(RevenueBudget.Cache, null, e.Row.RetainageMode == RetainageModes.Line);
				PXUIFieldAttribute.SetVisibility<PMRevenueBudget.curyCapAmount>(RevenueBudget.Cache, null, e.Row.RetainageMode == RetainageModes.Line ? PXUIVisibility.Visible : PXUIVisibility.Invisible);



				PXUIFieldAttribute.SetEnabled<PMOtherBudget.curyUnitRate>(OtherBudget.Cache, null, BudgetEditable());
				PXUIFieldAttribute.SetEnabled<PMOtherBudget.qty>(OtherBudget.Cache, null, BudgetEditable());
				PXUIFieldAttribute.SetEnabled<PMOtherBudget.curyAmount>(OtherBudget.Cache, null, BudgetEditable());

				PXUIFieldAttribute.SetVisible<PMProjectBalanceRecord.curyCommittedAmount>(BalanceRecords.Cache, null, Setup.Current.CostCommitmentTracking == true);
				PXUIFieldAttribute.SetVisible<PMProjectBalanceRecord.curyCommittedInvoicedAmount>(BalanceRecords.Cache, null, Setup.Current.CostCommitmentTracking == true);
				PXUIFieldAttribute.SetVisible<PMProjectBalanceRecord.curyCommittedOpenAmount>(BalanceRecords.Cache, null, Setup.Current.CostCommitmentTracking == true);
				PXUIFieldAttribute.SetVisible<PMProjectBalanceRecord.curyActualPlusOpenCommittedAmount>(BalanceRecords.Cache, null, Setup.Current.CostCommitmentTracking == true);
				PXUIFieldAttribute.SetVisible<PMProjectBalanceRecord.curyVarianceAmount>(BalanceRecords.Cache, null, Setup.Current.CostCommitmentTracking == true);
				PXUIFieldAttribute.SetVisible<PMProjectBalanceRecord.curyDraftCOAmount>(BalanceRecords.Cache, null, ChangeOrderVisible());
				PXUIFieldAttribute.SetVisible<PMProjectBalanceRecord.curyBudgetedCOAmount>(BalanceRecords.Cache, null, ChangeOrderVisible());
				PXUIFieldAttribute.SetVisible<PMProjectBalanceRecord.curyCommittedCOAmount>(BalanceRecords.Cache, null, Setup.Current.CostCommitmentTracking == true && ChangeOrderVisible());
				PXUIFieldAttribute.SetVisible<PMProjectBalanceRecord.curyOriginalCommittedAmount>(BalanceRecords.Cache, null, Setup.Current.CostCommitmentTracking == true);

				PXUIFieldAttribute.SetVisibility<PMProjectBalanceRecord.curyCommittedAmount>(BalanceRecords.Cache, null, Setup.Current.CostCommitmentTracking == true ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMProjectBalanceRecord.curyCommittedInvoicedAmount>(BalanceRecords.Cache, null, Setup.Current.CostCommitmentTracking == true ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMProjectBalanceRecord.curyCommittedOpenAmount>(BalanceRecords.Cache, null, Setup.Current.CostCommitmentTracking == true ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMProjectBalanceRecord.curyActualPlusOpenCommittedAmount>(BalanceRecords.Cache, null, Setup.Current.CostCommitmentTracking == true ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMProjectBalanceRecord.curyVarianceAmount>(BalanceRecords.Cache, null, Setup.Current.CostCommitmentTracking == true ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMProjectBalanceRecord.curyDraftCOAmount>(BalanceRecords.Cache, null, ChangeOrderVisible() ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMProjectBalanceRecord.curyBudgetedCOAmount>(BalanceRecords.Cache, null, ChangeOrderVisible() ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
				PXUIFieldAttribute.SetVisibility<PMProjectBalanceRecord.curyCommittedCOAmount>(BalanceRecords.Cache, null, ChangeOrderVisible() ? PXUIVisibility.Visible : PXUIVisibility.Invisible);
								
				PXDefaultAttribute.SetPersistingCheck<PMProject.dropshipExpenseSubMask>(e.Cache, e.Row, 
					PXAccess.FeatureInstalled<FeaturesSet.distributionModule>() && PXAccess.FeatureInstalled<FeaturesSet.subAccount>() ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);

				if (!PXAccess.FeatureInstalled<FeaturesSet.inventory>())
				{
					PXStringListAttribute.SetList<PMProject.dropshipExpenseAccountSource>(e.Cache, e.Row, 
						new string[] { 
							DropshipExpenseAccountSourceOption.PostingClassOrItem, 
							DropshipExpenseAccountSourceOption.Project, 
							DropshipExpenseAccountSourceOption.Task }, 
						new string[] { 
							Messages.AccountSource_Item,
							Messages.AccountSource_Project,
							Messages.AccountSource_Task });
				}

				if (e.Row.CustomerID != null)
				{
					PMAddress address = this.Billing_Address.Select();

					bool enableAddressValidation = address != null && address.IsDefaultAddress == false && address.IsValidated == false;
					validateAddresses.SetEnabled(enableAddressValidation);
				}
			}

			//sw.Stop();
			//Debug.Print("Project rowselected in {0} ms.", sw.ElapsedMilliseconds);

		}

		protected virtual void _(Events.RowInserted<PMProject> e)
		{
			var select = new PXSelect<NotificationSetup, Where<NotificationSetup.module, Equal<GL.BatchModule.modulePM>>>(this);
			
			bool NotificationSourcesCacheIsDirty = NotificationSources.Cache.IsDirty;
			foreach (NotificationSetup setup in select.Select())
			{
				NotificationSource source = new NotificationSource();
				source.SetupID = setup.SetupID;
				source.Active = setup.Active;
				source.EMailAccountID = setup.EMailAccountID;
				source.NotificationID = setup.NotificationID;
				source.ReportID = setup.ReportID;
				source.Format = setup.Format;


				NotificationSources.Insert(source);
			}

			NotificationSources.Cache.IsDirty = NotificationSourcesCacheIsDirty;

			if (e.Row != null)
			{
				ContractBillingSchedule schedule = new ContractBillingSchedule();
				schedule.ContractID = e.Row.ContractID;
				Billing.Insert(schedule);
				Billing.Cache.IsDirty = false;
			}
		}

		[Obsolete]
		protected virtual void _(Events.RowDeleting<PMProject> e)
		{
			}

		protected virtual void _(Events.RowDeleted<PMProject> e)
		{
			if (!string.IsNullOrEmpty(e.Row.QuoteNbr))
			{
				PMQuote quote = PXSelect<PMQuote, Where<PMQuote.quoteNbr, Equal<Required<PMQuote.quoteNbr>>>>.Select(this, e.Row.QuoteNbr);
				if (quote != null)
				{
					quote.QuoteProjectID = null;
					quote.Status = CRQuoteStatusAttribute.Approved;
					Quote.Update(quote);

					var selectQuoteDetails = new PXSelect<CROpportunityProducts, Where<CROpportunityProducts.quoteID, Equal<Required<CROpportunityProducts.quoteID>>>>(this);

					foreach (CROpportunityProducts item in selectQuoteDetails.Select(quote.QuoteID))
					{
						item.ProjectID = null;
						item.TaskID = null;
						QuoteDetails.Update(item);
					}
				}
			}
		}

		protected virtual void _(Events.RowInserted<NotificationSource> e)
		{
			bool NotificationRecipientsCacheIsDirty = NotificationRecipients.Cache.IsDirty;
			var select = new PXSelect<NotificationSetupRecipient, Where<NotificationSetupRecipient.setupID, Equal<Required<NotificationSetupRecipient.setupID>>>>(this);

			foreach (NotificationSetupRecipient setupRecipient in select.Select(e.Row.SetupID))
			{
				NotificationRecipient recipient = new NotificationRecipient();
				recipient.SetupID = setupRecipient.SetupID;
				recipient.Active = setupRecipient.Active;
				recipient.ContactID = setupRecipient.ContactID;
				recipient.AddTo = setupRecipient.AddTo;
				recipient.ContactType = setupRecipient.ContactType;
				recipient.Format = e.Row.Format;

				NotificationRecipients.Insert(recipient);
			}
			NotificationRecipients.Cache.IsDirty = NotificationRecipientsCacheIsDirty;
		}

		protected virtual void _(Events.FieldUpdated<PMProject, PMProject.customerID> e)
		{
			if (e.Row != null)
			{
				Customer customer = new PXSelect<Customer, Where<Customer.bAccountID, Equal<Required<Customer.bAccountID>>>>(this).Select(e.Row.CustomerID);

				if (string.IsNullOrEmpty(e.Row.QuoteNbr))
				{
					if (customer != null)
					{
						if (PXAccess.FeatureInstalled<FeaturesSet.projectMultiCurrency>() && !ProjectHasTransactions())
						{
							if (!string.IsNullOrEmpty(customer.CuryID))
								e.Cache.SetValueExt<PMProject.curyIDCopy>(e.Row, customer.CuryID);
							else
								e.Cache.SetDefaultExt<PMProject.curyIDCopy>(e.Row);
						}

						if (!string.IsNullOrEmpty(customer.CuryRateTypeID))
							e.Cache.SetValueExt<PMProject.rateTypeID>(e.Row, customer.CuryRateTypeID);
						else
						{
							e.Cache.SetDefaultExt<PMProject.rateTypeID>(e.Row);
							//CuryInfo.Cache.SetDefaultExt<CurrencyInfo.curyRateTypeID>(CuryInfo.Current);
						}
					}
				}
				try
				{
					PMAddressAttribute.DefaultRecord<PMProject.billAddressID>(e.Cache, e.Row);
					PMContactAttribute.DefaultRecord<PMProject.billContactID>(e.Cache, e.Row);
				}
				catch (PXFieldValueProcessingException ex)
				{
					throw new PXException(ex.Message);
				}

				e.Cache.SetDefaultExt<PMProject.termsID>(e.Row);
				e.Cache.SetDefaultExt<PMProject.locationID>(e.Row);

				foreach (PMTask task in Tasks.Select())
				{
					Tasks.Cache.SetDefaultExt<PMTask.customerID>(task);
					Tasks.Cache.SetDefaultExt<PMTask.locationID>(task);
					Tasks.Update(task);
				}

				if (Billing.Current != null && e.Row.CustomerID == null)
				{
					Billing.Current.Type = null;
					Billing.Current.NextDate = null;

					Billing.Update(Billing.Current);
				}

				if (Billing.Current != null && e.Row.CustomerID != null && Billing.Current.Type == null)
				{
					Billing.Current.Type = BillingType.Monthly;
					Billing.Update(Billing.Current);
				}

				if (PXAccess.FeatureInstalled<FeaturesSet.retainage>())
				{
					if (customer != null)
					{
						decimal? RetainageOldValue;
						RetainageOldValue = e.Row.RetainagePct;
						e.Row.RetainagePct = customer.RetainagePct;

						List<PMRevenueBudget> budgetToUpdate = new List<PMRevenueBudget>();

						foreach (PMRevenueBudget budget in RevenueBudget.Select())
						{
							if (budget.RetainagePct != Project.Current.RetainagePct)
							{
								budgetToUpdate.Add(budget);
							}
						}

						if (budgetToUpdate.Count > 0)
						{
							WebDialogResult result = Project.Ask(Messages.RetaiangeChangedDialogHeader, PXMessages.LocalizeFormatNoPrefix(Messages.RetaiangeChangedCustomerDialogQuestion, RetainageOldValue, Project.Current.RetainagePct), MessageButtons.YesNo, MessageIcon.Question);
							if (result == WebDialogResult.Yes)
							{
								foreach (PMRevenueBudget budget in budgetToUpdate)
								{
									budget.RetainagePct = Project.Current.RetainagePct;
									RevenueBudget.Update(budget);
								}
							}
						}
					}
				}
				Site_Address.Cache.SetDefaultExt<PMSiteAddress.countryID>(Site_Address.Cache.Current);
			}
		}

		protected virtual void _(Events.FieldVerifying<PMProject, PMProject.createProforma> e)
		{
			if (e.Row == null)
				return;

			if (e.Cache.GetStatus(e.Row) == PXEntryStatus.Inserted)
				return;

			var selectProforma = new PXSelect<PMProforma, Where<PMProforma.projectID, Equal<Required<PMProforma.projectID>>,
				And<PMProforma.released, Equal<False>>>>(this);

			PMProforma unreleasedProforma = selectProforma.SelectWindowed(0, 1, e.Row.ContractID);
			if (unreleasedProforma != null)
			{
				throw new PXSetPropertyException(Messages.UnreleasedProformaOrInvoice);
			}

			var selectInvoice = new PXSelect<ARInvoice, Where<ARInvoice.projectID, Equal<Required<PMProforma.projectID>>,
				And<ARInvoice.released, Equal<False>, And<ARInvoice.scheduled, Equal<False>, And<ARInvoice.voided, Equal<False>>>>>>(this);

			ARInvoice unreleasedInvoice = selectInvoice.SelectWindowed(0, 1, e.Row.ContractID);
			if (unreleasedInvoice != null)
			{
				throw new PXSetPropertyException(Messages.UnreleasedProformaOrInvoice);
			}

			if ((bool?)e.NewValue != true && e.Row.RetainageMode == RetainageModes.Contract)
			{
				throw new PXSetPropertyException<PMProject.createProforma>(Messages.ChangeRetainageMode);
			}
		}

		protected virtual void _(Events.FieldVerifying<PMProject, PMProject.changeOrderWorkflow> e)
		{
			if (e.Row == null)
				return;

			if (e.Cache.GetStatus(e.Row) == PXEntryStatus.Inserted)
				return;

			if (Setup.Current.CostCommitmentTracking != true)
				return;

			var selectPOLine = new PXSelect<PO.POLine, Where<PO.POLine.projectID, Equal<Required<PO.POLine.projectID>>,
				And<Where<PO.POLine.cancelled, Equal<False>, And<PO.POLine.completed, Equal<False>, And<PO.POLine.closed, Equal<False>>>>>>>(this);

			var selectChangeOrder = new PXSelect<PMChangeOrder, Where<PMChangeOrder.projectID, Equal<Required<PMChangeOrder.projectID>>>>(this);

			var selectChangeRequest = new PXSelect<PMChangeRequest, Where<PMChangeRequest.projectID, Equal<Required<PMChangeRequest.projectID>>>>(this);


			bool? newValue = (bool?)e.NewValue;

			if (newValue == true)
			{
				if (selectPOLine.SelectWindowed(0, 1, e.Row.ContractID).Any())
				{
					throw new PXSetPropertyException(Messages.CommitmentExistForThisProject_Enable);
				}
			}
			else
			{
				if (selectChangeOrder.SelectWindowed(0, 1, e.Row.ContractID).Any())
				{
					throw new PXSetPropertyException(Messages.ChangeOrderExistsForThisProject);
				}

				if (selectChangeRequest.SelectWindowed(0, 1, e.Row.ContractID).Any())
				{
					throw new PXSetPropertyException(Messages.ChangeRequestExistsForThisProject);
				}

				if (selectPOLine.SelectWindowed(0, 1, e.Row.ContractID).Any())
				{
					throw new PXSetPropertyException(Messages.CommitmentExistForThisProject_Cancel);
				}
			}
		}

		public override int ExecuteUpdate(string viewName, IDictionary keys, IDictionary values, params object[] parameters)
		{
			int intResult;

			if (values.Contains(nameof(PMProject.templateID)) && values[nameof(PMProject.templateID)] != null &&
				(Project.Current != null && Project.Cache.GetStatus(Project.Current) == PXEntryStatus.Inserted && !IsCopyPaste && Project.Current.QuoteNbr == null || this.IsMobile))
			{
				var oldTemplateID = Project.Current.TemplateID;

				intResult = base.ExecuteUpdate(viewName, keys, values, parameters);

				var newTempleteID = Project.Current.TemplateID;

				if (newTempleteID != null && newTempleteID != oldTemplateID)
				{
					if (!this.IsMobile && !this.IsImport && IsLargeTemplate(newTempleteID.Value))
					{
						LoadFromTemplateDialog.Current.TemplateID = newTempleteID;
						LoadFromTemplateDialog.AskExt();
					}
					else
					{
						_isLoadFromTemplate = false;
						DefaultFromTemplate(Project.Current, newTempleteID, DefaultFromTemplateSettings.Default);
					}
				}
			}
			else
			{
				intResult = base.ExecuteUpdate(viewName, keys, values, parameters);
			}

			return intResult;
		}

		[Obsolete]
		protected virtual void _(Events.FieldUpdated<PMProject, PMProject.templateID> e)
		{
		}

		private bool IsLargeTemplate(int templateID)
		{
			int largeProjectTemplateSize = Setup.Current.LargeProjectTemplateSize ?? 1000;

			int intTaskCount = PXSelect<PMTask, Where<PMTask.projectID, Equal<Required<PMTask.projectID>>>>.Select(this, templateID).Count();
			if (intTaskCount >= largeProjectTemplateSize)
			{
				return true;
			}

			int intBudgetCount = PXSelect<PMBudget, Where<PMBudget.projectID, Equal<Required<PMBudget.projectID>>>>.Select(this, templateID).Count();
			if (intTaskCount + intBudgetCount >= largeProjectTemplateSize)
			{
				return true;
			}

			return false;
		}

		public PXAction<PMProject> loadFromTemplate;
		[PXUIField(DisplayName = "Proceed", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Select, Enabled = false)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.Process)]
		public virtual IEnumerable LoadFromTemplate(PXAdapter adapter)
		{
			PMProject templ = PMProject.PK.Find(this, LoadFromTemplateDialog.Current.TemplateID);
			if (templ != null)
			{
				if (DefaultFromTemplateSettings.Default.CopyProperties)
				{
					DefaultFromTemplateProjectSettings(this.Project.Current, templ);
				}
				this.Project.Update(Project.Current);
				string oldProjectCD = Project.Current.ContractCD;
				this.Save.Press();

				PXLongOperation.StartOperation(this, delegate ()
				{
					ProjectEntry pe = PXGraph.CreateInstance<ProjectEntry>();
					pe.Project.Current = (PMProject)Project.Cache.CreateCopy(Project.Current);
					pe.Project.Update(pe.Project.Current);
					pe._isLoadFromTemplate = true;
					pe.DefaultFromTemplate(pe.Project.Current, templ.ContractID, DefaultFromTemplateSettings.Default);
					pe._isLoadFromTemplate = false;
					pe.Save.Press();
					throw new PXRedirectRequiredException(pe, null);
				});

				if (Project.Current.ContractCD != oldProjectCD)
				{
					throw new PXRedirectRequiredException(this, null);
				}
			}

			return adapter.Get();
		}

		protected virtual void _(Events.FieldUpdated<PMProject, PMProject.locationID> e)
		{
			if (e.Row != null)
			{
				e.Cache.SetDefaultExt<PMProject.defaultSalesSubID>(e.Row);
			}
		}

		protected virtual void _(Events.FieldUpdated<PMProject, PMProject.ownerID> e)
        {
			e.Cache.SetDefaultExt<PMProject.approverID>(e.Row);
        }

		protected virtual void _(Events.FieldDefaulting<PMProject, PMProject.approverID> e)
        {
			PX.Objects.CR.Standalone.EPEmployee owner = PXSelect<PX.Objects.CR.Standalone.EPEmployee, Where<PX.Objects.CR.Standalone.EPEmployee.defContactID, Equal<Current<PMProject.ownerID>>>>.Select(this);
			if (owner != null)
            {
				e.NewValue = owner.BAccountID;
            }
		}

		protected virtual void _(Events.FieldUpdated<PMProject, PMProject.contractCD> e)
        {
			OnProjectCDChanged();
		}

		private bool ProjectHasTransactions()
		{
			bool result;
			using (new PXReadBranchRestrictedScope())
			{
				var select = new PXSelectReadonly<PMTran,
				Where<PMTran.projectID, Equal<Current<PMProject.contractID>>>>(this);
				result = select.SelectWindowed(0, 1).Any();
			}

			return result;
		}

		protected virtual void _(Events.FieldDefaulting<PMProject, PMProject.costBudgetLevel> e)
		{
			if (CostCodeAttribute.UseCostCode())
			{
				e.NewValue = BudgetLevels.CostCode;
			}
			else
			{
				e.NewValue = BudgetLevels.Item;
			}
		}

		protected virtual void _(Events.FieldDefaulting<PMProject, PMProject.billingCuryID> e)
		{
			if (e.Row != null)
				e.NewValue = e.Row.CuryIDCopy;
		}

		protected virtual void _(Events.FieldVerifying<PMProject, PMProject.billingCuryID> e)
		{
			if (e.Row != null && !string.IsNullOrEmpty(e.Row.BaseCuryID) && e.Row.CuryIDCopy != e.Row.BaseCuryID && e.NewValue as string != e.Row.CuryIDCopy)
				throw new PXSetPropertyException<PMProject.billingCuryID>(Messages.BillingCuryCannotBeChanged, e.Row.CuryIDCopy, e.Row.BaseCuryID);
		}

		protected virtual void _(Events.FieldVerifying<PMProject, PMProject.retainagePct> e)
		{
			if (e.Row != null && e.NewValue != null)
			{
				decimal percent = (decimal) e.NewValue;
				if (percent < 0 || percent > 100)
				{
					throw new PXSetPropertyException<PMProject.retainagePct>(IN.Messages.PercentageValueShouldBeBetween0And100);
				}
					
			}
		}

		protected virtual void _(Events.FieldDefaulting<PMProject, PMProject.accountingMode> e)
		{
			if (e.Row != null)
			{
				e.NewValue = PXAccess.FeatureInstalled<FeaturesSet.materialManagement>() ? ProjectAccountingModes.ProjectSpecific : ProjectAccountingModes.Linked;
			}
		}

		protected virtual void _(Events.FieldVerifying<PMProject, PMProject.accountingMode> e)
		{
			if (e.Row != null)
			{
				VerifyModeForLinkedLocations((string)e.NewValue);

				if (e.NewValue != e.OldValue)
                {
					VerifyNoItemsOnHand();
				}
			}
		}

		private void VerifyModeForLinkedLocations(string newMode)
        {
			var selectLinked = new PXSelect<INLocation, Where<INLocation.projectID, Equal<Current<PMProject.contractID>>>>(this);
			INLocation linkedLocation = selectLinked.SelectSingle();
			if (linkedLocation != null && newMode != ProjectAccountingModes.Linked)
			{
				throw new PXSetPropertyException<PMProject.accountingMode>(Messages.ModeNotValid_Linked);
			}
		}

		private void VerifyNoItemsOnHand()
		{
			var select = new PXSelect<PMSiteSummaryStatus,
				Where<PMSiteSummaryStatus.projectID, Equal<Current<PMProject.contractID>>,
				And<PMSiteSummaryStatus.qtyOnHand, NotEqual<decimal0>>>>(this);

			PMSiteSummaryStatus any = select.SelectSingle();
			if (any != null)
            {
				throw new PXSetPropertyException<PMProject.accountingMode>(Messages.ModeNotValid_Linked);
			}
		}

		protected virtual void _(Events.FieldUpdated<PMProject, PMProject.retainagePct> e)
		{
			e.Cache.SetDefaultExt<PMProject.curyCapAmount>(e.Row);

			if (PXAccess.FeatureInstalled<FeaturesSet.retainage>())
				updateRetainage.Press();
		}

		protected virtual void _(Events.FieldDefaulting<CurrencyInfo, CurrencyInfo.curyEffDate> e)
		{
			if (Project.Cache.Current != null)
			{
				e.NewValue = Project.Current.StartDate;
				e.Cancel = true;
			}
		}

		protected virtual void _(Events.FieldDefaulting<CurrencyInfo, CurrencyInfo.curyRateTypeID> e)
		{
			if (IsCopyPaste && CopySource.Project.Current != null && !string.IsNullOrEmpty(CopySource.Project.Current.RateTypeID))
			{
				e.NewValue = CopySource.Project.Current.RateTypeID;
				e.Cancel = true;
			}
		}

		protected virtual void _(Events.FieldUpdated<PMProject, PMProject.startDate> e)
		{
			if (e.Row != null && CuryInfo.Current != null && PXAccess.FeatureInstalled<FeaturesSet.projectMultiCurrency>())
			{
				CuryInfo.Current.CuryEffDate = e.Row.StartDate;
				CuryInfo.Update(CuryInfo.Current);

				ShowWaringOnProjectCurrecyIfExcahngeRateNotFound(e.Row);
			}
		}

		protected virtual void _(Events.FieldUpdated<PMProject, PMProject.rateTypeID> e)
		{
			if (e.Row != null && CuryInfo.Current != null && PXAccess.FeatureInstalled<FeaturesSet.projectMultiCurrency>())
			{
				CuryInfo.Current.CuryRateTypeID = e.Row.RateTypeID ?? CMSetup.Current.PMRateTypeDflt;
				CuryInfo.Update(CuryInfo.Current);

				if (e.Row.StartDate != null)
				{
					ShowWaringOnProjectCurrecyIfExcahngeRateNotFound(e.Row);
				}
			}
		}

		private void ShowWaringOnProjectCurrecyIfExcahngeRateNotFound(PMProject row)
		{
			if (CuryInfo.Current != null)
			{
				PXUIFieldAttribute.SetError<CurrencyInfo.curyEffDate>(CuryInfo.Cache, CuryInfo.Current, null);
				CuryInfo.Cache.RaiseFieldUpdated<CurrencyInfo.curyEffDate>(CuryInfo.Current, row.StartDate);
				string internalError = PXUIFieldAttribute.GetError<CurrencyInfo.curyEffDate>(CuryInfo.Cache, CuryInfo.Current);
				if (!string.IsNullOrEmpty(internalError))
					Project.Cache.RaiseExceptionHandling<PMProject.curyIDCopy>(row, null,
						new PXSetPropertyException(Messages.CurrencyRateIsNotDefined, PXErrorLevel.Warning,
					CuryInfo.Current.CuryID, CuryInfo.Current.BaseCuryID, CuryInfo.Current.CuryRateTypeID, CuryInfo.Current.CuryEffDate)
						);
			}
		}
				
		protected virtual void _(Events.FieldUpdated<PMProject, PMProject.steppedRetainage> e)
		{
			if (!IsCopyPaste && e.Row.SteppedRetainage == true && RetainageSteps.Select().Count == 0)
			{
				PMRetainageStep step = RetainageSteps.Insert();
				step.ThresholdPct = 0;
				step.RetainagePct = ((PMProject)e.Row).RetainagePct;
				RetainageSteps.Update(step);
				SyncRetainage();
			}
		}

		protected virtual void _(Events.FieldVerifying<PMProject, PMProject.retainageMode> e)
		{
			if ((string)e.NewValue == RetainageModes.Contract && e.Row.CreateProforma != true)
			{
				throw new PXSetPropertyException<PMProject.retainageMode>(Messages.CreateProformaRequired);
			}
		}
				
		protected virtual void _(Events.FieldUpdated<PMProject, PMProject.retainageMode> e)
		{
			if (e.Row.RetainageMode == RetainageModes.Line)
			{
				e.Row.SteppedRetainage = false;
			}

			if (e.Row.RetainageMode == RetainageModes.Contract)
			{
				SyncRetainage();
			}
		}

		protected virtual void _(Events.FieldDefaulting<PMProject, PMProject.curyCapAmount> e)
		{
			e.NewValue = CalculateCapAmount(e.Row, (PMProjectRevenueTotal)ProjectRevenueTotals.View.SelectSingleBound(new object[] { e.Row }));
		}
		
		protected virtual void _(Events.FieldUpdated<PMProject, PMProject.retainageMaxPct> e)
		{
			e.Cache.SetDefaultExt<PMProject.curyCapAmount>(e.Row);
		}

		protected virtual void _(Events.FieldUpdated<PMProject, PMProject.includeCO> e)
		{
			e.Cache.SetDefaultExt<PMProject.curyCapAmount>(e.Row);
		}

		protected virtual void _(Events.FieldDefaulting<PMProject, PMProject.baseCuryID> e)
		{
			if(e.Row != null && !IsCopyPaste)
			{
				GL.Branch branch = null;
				if (e.Row.DefaultBranchID != null)
				{
					branch = GL.Branch.PK.Find(this, e.Row.DefaultBranchID);
				}
				e.NewValue = branch?.BaseCuryID ?? Accessinfo.BaseCuryID ?? Company.Current.BaseCuryID;
			}
		}

		protected virtual void _(Events.FieldDefaulting<PMProject, PMProject.defaultBranchID> e)
		{
			if (IsCopyPaste)
			{
				e.Cancel = true;
			}
		}

		protected virtual void _(Events.FieldVerifying<PMProject, PMProject.baseCuryID> e)
		{
			string newBaseCuryID = e.NewValue as string;
			if (e.Row != null && !string.IsNullOrEmpty(e.Row.BaseCuryID)
				&& e.Row.BaseCuryID != newBaseCuryID
				&& !string.IsNullOrEmpty(newBaseCuryID))
			{
				if (e.Row.DefaultBranchID != null)
				{
					GL.Branch branch = GL.Branch.PK.Find(this, e.Row.DefaultBranchID);
					if (branch != null && branch.BaseCuryID != e.NewValue as string)
						throw new PXSetPropertyException<PMProject.baseCuryID>(Messages.CannotSelectCuryDiffersFromBranchBaseCury);
				}
					
				if (ProjectHasTransactions())
					throw new PXSetPropertyException<PMProject.baseCuryID>(Messages.BaseCuryCannotBeChanged);
			}
		}

		protected virtual void _(Events.FieldUpdated<PMProject, PMProject.baseCuryID> e)
		{
			if (e.Row == null) return;

			string newCuryID = e.Row.BaseCuryID ?? Accessinfo.BaseCuryID ?? Company.Current.BaseCuryID;
			CurrencyInfo projectCuryInfo = CuryInfo.Search<CurrencyInfo.curyInfoID>(e.Row.CuryInfoID);
			if (projectCuryInfo != null && projectCuryInfo.BaseCuryID != newCuryID)
			{
				projectCuryInfo.BaseCuryID = newCuryID;
				//needed for CuryInfo recalculation with changed BaseCuryID
				projectCuryInfo.CuryEffDate = DateTime.MinValue;
				CuryInfo.Cache.Update(projectCuryInfo);
				projectCuryInfo.CuryEffDate = Accessinfo.BusinessDate;
				CuryInfo.Cache.Update(projectCuryInfo);

				ShowWaringOnProjectCurrecyIfExcahngeRateNotFound(e.Row);
			}

			if (!PXAccess.FeatureInstalled<FeaturesSet.projectMultiCurrency>())
			{
				e.Cache.SetDefaultExt<PMProject.curyIDCopy>(e.Row);
			}
		}

		protected virtual void _(Events.FieldVerifying<PMProject, PMProject.defaultBranchID> e)
		{
			if (e.Row != null && e.NewValue != null && e.Row.BaseCuryID != e.NewValue as string)
			{
				GL.Branch branch = GL.Branch.PK.Find(this, (int?) e.NewValue);
				if (branch.BaseCuryID != e.Row.BaseCuryID && ProjectHasTransactions())
				{
					var ex = new PXSetPropertyException(Messages.CannotSelectCuryDiffersFromProjectBaseCury, e.Row.BaseCuryID);
					// Acuminator disable once PX1047 RowChangesInEventHandlersForbiddenForArgs Acuminator is wrong. There is no DAC instance modification.
					ex.ErrorValue = branch.BranchCD;

					throw ex;
				}
			}
		}


		protected virtual void _(Events.FieldUpdated<PMProject, PMProject.defaultBranchID> e)
		{
			if (e.Row != null && e.Row.DefaultBranchID != null)
			{
				e.Cache.SetDefaultExt<PMProject.baseCuryID>(e.Row);
			}
		}

		#endregion

		protected virtual void _(Events.RowSelected<EPEmployeeContract> e)
		{
			ContractRates.AllowInsert = e.Row != null;
		}

		protected virtual void _(Events.RowUpdated<EPEmployeeContract> e)
		{
			if (e.OldRow == null)
				return;

			PXResultset<EPContractRate> res = PXSelect<EPContractRate , Where<EPContractRate.contractID, Equal<Current<PMProject.contractID>>
										, And<EPContractRate.employeeID, Equal<Required<EPContractRate.employeeID>>>>>.Select(this, e.OldRow.EmployeeID);
			foreach (EPContractRate contractRate in res)
			{
				contractRate.EmployeeID = e.Row.EmployeeID;
				ContractRates.Update(contractRate);
			}

					
			EPContractRate.UpdateKeyFields(this, e.OldRow.ContractID, e.OldRow.EmployeeID, e.Row.ContractID, e.Row.EmployeeID);
		}


		#region Mobile related to be reviewed in next version

		protected virtual void EPEmployeeContract_RowInserting(PXCache sender, PXRowInsertingEventArgs e)
		{
			EPEmployeeContract row = e.Row as EPEmployeeContract;
			if (row != null && this.IsMobile)
			{
				var toDelete = new List<object>();
				foreach (var item in sender.Inserted)
				{
					if (((EPEmployeeContract)item).EmployeeID == null)
					{
						toDelete.Add(item);
					}
				}

				foreach (var item in toDelete)
				{
					sender.Delete(item);
				}
			}
		}

		protected virtual void EPEmployeeContract_RowUpdating(PXCache sender, PXRowUpdatingEventArgs e)
		{
			EPEmployeeContract row = e.Row as EPEmployeeContract;
			if (row != null && this.IsMobile)
			{
				var toDelete = new List<object>();
				foreach (var item in sender.Inserted)
				{
					if (((EPEmployeeContract)item).EmployeeID == row.EmployeeID)
					{
						toDelete.Add(item);
					}
				}

				foreach (var item in toDelete)
				{
					sender.Delete(item);
				}
			}
		}

		protected virtual void EPEmployeeContract_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			EPEmployeeContract row = e.Row as EPEmployeeContract;
			if (row != null && this.IsMobile && e.Operation != PXDBOperation.Delete)
			{
				if (row.EmployeeID == null)
				{
					sender.Delete(row);
					e.Cancel = true;
				}
			}
		}

		protected virtual void EPEquipmentRate_EquipmentID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			if (this.IsMobile && e.Row != null)
			{
				EPEquipmentRate row = e.Row as EPEquipmentRate;
				if ((int?)e.OldValue != row.EquipmentID)
				{
					sender.SetDefaultExt<EPEquipmentRate.runRate>(row);
					sender.SetDefaultExt<EPEquipmentRate.setupRate>(row);
					sender.SetDefaultExt<EPEquipmentRate.suspendRate>(row);
				}
			}
		}

		protected virtual void EPEquipmentRate_RowInserting(PXCache sender, PXRowInsertingEventArgs e)
		{
			EPEquipmentRate row = e.Row as EPEquipmentRate;
			if (row != null && this.IsMobile)
			{
				var toDelete = new List<object>();
				foreach (var item in sender.Inserted)
				{
					if (((EPEquipmentRate)item).EquipmentID == null)
					{
						toDelete.Add(item);
					}
				}

				foreach (var item in toDelete)
				{
					sender.Delete(item);
				}
			}
		}

		protected virtual void EPEquipmentRate_RowUpdating(PXCache sender, PXRowUpdatingEventArgs e)
		{
			EPEquipmentRate row = e.Row as EPEquipmentRate;
			if (row != null && this.IsMobile)
			{
				var toDelete = new List<object>();
				foreach (var item in sender.Inserted)
				{
					if (((EPEquipmentRate)item).EquipmentID == row.EquipmentID)
					{
						toDelete.Add(item);
					}
				}

				foreach (var item in toDelete)
				{
					sender.Delete(item);
				}
			}
		}

		protected virtual void EPEquipmentRate_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			EPEquipmentRate row = e.Row as EPEquipmentRate;
			if (row != null && this.IsMobile && e.Operation != PXDBOperation.Delete)
			{
				if (row.EquipmentID == null)
				{
					sender.Delete(row);
					e.Cancel = true;
				}
			}
		}

		#endregion


		#region Revenue Budget
		protected virtual void _(Events.RowSelected<PMRevenueBudget> e)
		{
			if (e.Row != null)
			{
				PXUIFieldAttribute.SetEnabled<PMRevenueBudget.limitQty>(e.Cache, e.Row, !string.IsNullOrEmpty(e.Row.UOM));
				PXUIFieldAttribute.SetEnabled<PMRevenueBudget.maxQty>(e.Cache, e.Row, e.Row.LimitQty == true && !string.IsNullOrEmpty(e.Row.UOM));
				PXUIFieldAttribute.SetEnabled<PMRevenueBudget.curyMaxAmount>(e.Cache, e.Row, e.Row.LimitAmount == true);

				bool isRevenueQtyVisible = RevenueQuantityVisible();
				PXUIFieldAttribute.SetVisible<PMRevenueBudget.qtyToInvoice>(e.Cache, e.Row, isRevenueQtyVisible);
				if (isRevenueQtyVisible)
				{
					if ((e.Row.Qty != 0 || e.Row.RevisedQty != 0) && string.IsNullOrEmpty(e.Row.UOM))
					{
						if (string.IsNullOrEmpty(PXUIFieldAttribute.GetError<PMRevenueBudget.uOM>(e.Cache, e.Row)))
							PXUIFieldAttribute.SetWarning<PMRevenueBudget.uOM>(e.Cache, e.Row, Messages.UomNotDefinedForBudget);
					}
					else
					{
						string errorText = PXUIFieldAttribute.GetError<PMRevenueBudget.uOM>(e.Cache, e.Row);
						if (errorText == PXLocalizer.Localize(Messages.UomNotDefinedForBudget))
						{
							PXUIFieldAttribute.SetWarning<PMRevenueBudget.uOM>(e.Cache, e.Row, null);
						}
					}
				}
				else
				{
					PXUIFieldAttribute.SetWarning<PMRevenueBudget.uOM>(e.Cache, e.Row, null);
				}

				PXUIFieldAttribute.SetEnabled<PMRevenueBudget.curyAmountToInvoice>(e.Cache, e.Row, e.Row.ProgressBillingBase == ProgressBillingBase.Amount);
				PXUIFieldAttribute.SetEnabled<PMRevenueBudget.qtyToInvoice>(e.Cache, e.Row, e.Row.ProgressBillingBase == ProgressBillingBase.Quantity);

				PXUIFieldAttribute.SetVisible<PMRevenueBudget.progressBillingBase>(e.Cache, e.Row, e.Cache.Graph.IsImportFromExcel || e.Cache.Graph.IsExport);
			}
		}

		protected virtual void _(Events.FieldUpdated<PMRevenueBudget, PMRevenueBudget.curyAmount> e)
		{
			if (e.Row != null)
			{
				e.Row.CuryRevisedAmount = e.Row.CuryAmount + e.Row.CuryChangeOrderAmount;

				try
				{
					_BlockQtyToInvoiceCalculate = true;

				RecalculateRevenueBudget(e.Row);
			}
				finally
				{
					_BlockQtyToInvoiceCalculate = false;
				}
			}
		}

		protected virtual void _(Events.FieldUpdated<PMRevenueBudget, PMRevenueBudget.qty> e)
		{
			if (e.Row != null)
			{
				e.Row.RevisedQty = e.Row.Qty;
			}

			e.Cache.SetDefaultExt<PMRevenueBudget.curyUnitRate>(e.Row);

			RecalculateRevenueBudget(e.Row);
		}

		protected virtual void _(Events.FieldUpdated<PMRevenueBudget, PMRevenueBudget.costCodeID> e)
		{
			e.Cache.SetDefaultExt<PMRevenueBudget.description>(e.Row);
		}

        protected virtual void _(Events.FieldUpdated<PMRevenueBudget, PMRevenueBudget.inventoryID> e)
        {
            e.Cache.SetDefaultExt<PMRevenueBudget.description>(e.Row);

            if (e.Row.AccountGroupID == null)
                e.Cache.SetDefaultExt<PMRevenueBudget.accountGroupID>(e.Row);

            e.Cache.SetDefaultExt<PMRevenueBudget.uOM>(e.Row);
            e.Cache.SetDefaultExt<PMRevenueBudget.curyUnitRate>(e.Row);
            e.Cache.SetDefaultExt<PMRevenueBudget.taxCategoryID>(e.Row);
        }

		protected virtual void _(Events.FieldUpdated<PMRevenueBudget, PMRevenueBudget.uOM> e)
		{
			e.Cache.SetDefaultExt<PMRevenueBudget.curyUnitRate>(e.Row);
		}

		protected virtual void _(Events.FieldUpdated<PMRevenueBudget, PMRevenueBudget.progressBillingBase> e)
		{
			if(e.Row != null)
			{
				string newValue = e.NewValue as string;
				if(newValue == ProgressBillingBase.Amount)
				{
					e.Cache.SetValueExt<PMRevenueBudget.qtyToInvoice>(e.Row, 0.0m);
					e.Cache.RaiseFieldUpdated<PMRevenueBudget.completedPct>(e.Row, 0.0m);
				}
				else if (newValue == ProgressBillingBase.Quantity)
				{
					e.Cache.SetValueExt<PMRevenueBudget.curyAmountToInvoice>(e.Row, 0.0m);
					e.Cache.RaiseFieldUpdated<PMRevenueBudget.completedPct>(e.Row, 0.0m);
				}
			}
		}

		protected virtual void _(Events.FieldDefaulting<PMRevenueBudget, PMRevenueBudget.description> e)
		{
			if (e.Row == null || Project.Current == null) return;

			if (Project.Current.BudgetLevel == BudgetLevels.CostCode || Project.Current.BudgetLevel == BudgetLevels.Detail)
			{
				if (e.Row.CostCodeID != null)
				{
					PMCostCode costCode = PXSelectorAttribute.Select<PMRevenueBudget.costCodeID>(e.Cache, e.Row) as PMCostCode;
					if (costCode != null)
					{
						e.NewValue = costCode.Description;
					}
				}
			}
			else if (Project.Current.BudgetLevel == BudgetLevels.Task)
			{
				if (e.Row.ProjectTaskID != null)
				{
					PMTask projectTask = PXSelectorAttribute.Select<PMRevenueBudget.projectTaskID>(e.Cache, e.Row) as PMTask;
					if (projectTask != null)
					{
						e.NewValue = projectTask.Description;
					}
				}
			}
			else if(Project.Current.BudgetLevel == BudgetLevels.Item)
			{
				if (e.Row.InventoryID != null && e.Row.InventoryID != PMInventorySelectorAttribute.EmptyInventoryID)
				{
					InventoryItem item = PXSelectorAttribute.Select<PMRevenueBudget.inventoryID>(e.Cache, e.Row) as InventoryItem;
					if (item != null)
					{
						e.NewValue = item.Descr;
					}
				}
			}
		}

		protected virtual void _(Events.FieldDefaulting<PMRevenueBudget, PMRevenueBudget.accountGroupID> e)
		{
			if (e.Row == null) return;
			if (IsCopyPasteContext) return; //do not guess keys - they will be supplied.

			var select = new PXSelect<PMAccountGroup, Where<PMAccountGroup.type, Equal<GL.AccountType.income>>>(this);

			var resultset = select.SelectWindowed(0, 2);

			if (resultset.Count == 1)
			{
				e.NewValue = ((PMAccountGroup)resultset).GroupID;
			}
			else
			{
				if (e.Row.InventoryID != null && e.Row.InventoryID != PMInventorySelectorAttribute.EmptyInventoryID)
				{
					InventoryItem item = PXSelectorAttribute.Select<PMRevenueBudget.inventoryID>(e.Cache, e.Row) as InventoryItem;
					if (item != null)
					{
						Account account = PXSelectorAttribute.Select<InventoryItem.salesAcctID>(Caches[typeof(InventoryItem)], item) as Account;
						if (account != null && account.AccountGroupID != null)
						{
							e.NewValue = account.AccountGroupID;
						}
					}
				}
			}
		}

		protected virtual void _(Events.FieldUpdated<PMRevenueBudget, PMRevenueBudget.projectTaskID> e)
		{
			e.Cache.SetDefaultExt<PMRevenueBudget.description>(e.Row);
			e.Cache.SetDefaultExt<PMRevenueBudget.taxCategoryID>(e.Row);

			if (e.Row != null && e.NewValue != null && (!e.Cache.Graph.IsImportFromExcel || e.Row.ProgressBillingBase == null))
			{
				PMTask task = PMTask.PK.FindDirty(this, e.Row.ProjectID, e.NewValue as int?);

				if (task != null)
				{
					e.Cache.SetValueExt<PMRevenueBudget.progressBillingBase>(e.Row, task.ProgressBillingBase);
				}
			}
		}

		protected virtual void _(Events.FieldDefaulting<PMRevenueBudget, PMRevenueBudget.inventoryID> e)
		{
			e.NewValue = PMInventorySelectorAttribute.EmptyInventoryID;
		}

		protected virtual void _(Events.FieldDefaulting<PMRevenueBudget, PMRevenueBudget.costCodeID> e)
		{
			if (Project.Current != null)
			{
				if (Project.Current.BudgetLevel != BudgetLevels.CostCode)
				{
					e.NewValue = CostCodeAttribute.GetDefaultCostCode();
				}
			}
		}

		protected virtual void _(Events.FieldDefaulting<PMRevenueBudget, PMRevenueBudget.projectTaskID> e)
		{
			if (RevenueFilter.Current != null && RevenueFilter.Current.ProjectTaskID != null)
			{
				e.NewValue = RevenueFilter.Current.ProjectTaskID;
			}
		}

		protected virtual void _(Events.FieldUpdated<PMRevenueBudget, PMRevenueBudget.curyUnitRate> e)
		{
			if (e.Row != null)
			{
				e.Row.CuryRevisedAmount = e.Row.CuryAmount + e.Row.CuryChangeOrderAmount;

				if(e.Row.ProgressBillingBase == ProgressBillingBase.Quantity)
				{
					e.Row.CuryAmountToInvoice = e.Row.QtyToInvoice.GetValueOrDefault() * e.Row.CuryUnitRate.GetValueOrDefault();
				}
			}
		}

		protected virtual void _(Events.FieldDefaulting<PMRevenueBudget, PMRevenueBudget.curyUnitRate> e)
		{
			if (Project.Current != null)
			{
				decimal? unitPrice = RateService.CalculateUnitPrice(e.Cache, e.Row.ProjectID, e.Row.ProjectTaskID, e.Row.InventoryID, e.Row.UOM, e.Row.Qty, Project.Current.StartDate, Project.Current.CuryInfoID);
				if (unitPrice != null)
					e.NewValue = unitPrice;
				else
					e.NewValue = e.Row.CuryUnitRate;
			}
		}

		protected virtual void _(Events.FieldDefaulting<PMRevenueBudget, PMRevenueBudget.taxCategoryID> e)
		{
			if (Project.Current != null)
			{
				if (e.Row.InventoryID != null && e.Row.InventoryID != PMInventorySelectorAttribute.EmptyInventoryID)
				{
					InventoryItem item = (InventoryItem)PXSelectorAttribute.Select<PMRevenueBudget.inventoryID>(e.Cache, e.Row);
					if (item != null && item.TaxCategoryID != null)
					{
						e.NewValue = item.TaxCategoryID;
					}
				}
				
				if (e.NewValue == null)
				{
					PMTask task = PMTask.PK.FindDirty(this, e.Row.ProjectID, e.Row.ProjectTaskID);
					if (task != null && task.TaxCategoryID != null)
					{
						e.NewValue = task.TaxCategoryID;
					}
				}
			}
		}

		protected virtual void _(Events.FieldDefaulting<PMProject, PMProject.dropshipExpenseSubMask> e)
		{
			if (Setup.Current != null)
			{
				e.NewValue = Setup.Current.DropshipExpenseSubMask;
				if (e.NewValue != null)
					e.Cancel = true;
			}
		}

		protected virtual void _(Events.FieldSelecting<PMRevenueBudget, PMRevenueBudget.completedPct> e)
		{
			if (e.Row != null && Setup.Current.AutoCompleteRevenueBudget == true)
			{
				decimal value = Math.Round(CalculateCompletedPercentByCost(e.Row), PMRevenueBudget.completedPct.Precision);
				
				PXFieldState fieldState = PXDecimalState.CreateInstance(value, PMRevenueBudget.completedPct.Precision, nameof(PMRevenueBudget.CompletedPct), false, 0, Decimal.MinValue, Decimal.MaxValue);
				fieldState.Enabled = false;
				e.ReturnState = fieldState;
			}
		}

		protected virtual void _(Events.FieldUpdated<PMRevenueBudget, PMRevenueBudget.completedPct> e)
		{
			RecalculateRevenueBudget(e.Row);
		}

		protected virtual void _(Events.FieldUpdated<PMRevenueBudget, PMRevenueBudget.curyRevisedAmount> e)
		{
			try
			{
				_BlockQtyToInvoiceCalculate = true;

			RecalculateRevenueBudget(e.Row);
		}
			finally
			{
				_BlockQtyToInvoiceCalculate = false;
			}
		}

		protected virtual void _(Events.FieldUpdated<PMRevenueBudget, PMRevenueBudget.curyAmountToInvoice> e)
		{
			if (!_IsRecalculatingRevenueBudgetScope && e.Row?.ProgressBillingBase == ProgressBillingBase.Amount) //The value is manually edited by the user on the screen
			{
				//Sync the Completed %
				decimal budgetedAmount = e.Row.CuryRevisedAmount.GetValueOrDefault();
				if (budgetedAmount != 0)
				{
					decimal invoicedOrPendingPrepayment = e.Row.CuryAmountToInvoice.GetValueOrDefault() + e.Row.CuryActualAmount.GetValueOrDefault() + e.Row.CuryInvoicedAmount.GetValueOrDefault() + (e.Row.CuryPrepaymentAmount.GetValueOrDefault() - e.Row.CuryPrepaymentInvoiced.GetValueOrDefault());
					decimal completedPct = 100m * invoicedOrPendingPrepayment / budgetedAmount;

					e.Row.CompletedPct = decimal.Round(completedPct, PMRevenueBudget.completedPct.Precision);
				}
			}
		}

		private bool _BlockQtyToInvoiceCalculate = false;

		protected virtual void _(Events.FieldUpdated<PMRevenueBudget, PMRevenueBudget.qtyToInvoice> e)
		{
			var budgetLine = e.Row;
			if (!_IsRecalculatingRevenueBudgetScope && budgetLine?.ProgressBillingBase == ProgressBillingBase.Quantity) //The value is manually edited by the user on the screen
			{
				decimal revisedQty = budgetLine.RevisedQty.GetValueOrDefault();
				if (revisedQty != 0.0m)
				{
					decimal qtySum = budgetLine.InvoicedQty.GetValueOrDefault()
						+ budgetLine.ActualQty.GetValueOrDefault()
						+ budgetLine.QtyToInvoice.GetValueOrDefault();

					decimal completedPct = 100m * qtySum / revisedQty;

					try
					{
						_BlockQtyToInvoiceCalculate = true;

					e.Cache.SetValueExt<PMRevenueBudget.completedPct>(e.Row, decimal.Round(completedPct, PMRevenueBudget.completedPct.Precision));
				}
					finally
					{
						_BlockQtyToInvoiceCalculate = false;
					}
				}
			}
		}

		protected virtual void _(Events.FieldSelecting<PMRevenueBudget, PMRevenueBudget.prepaymentPct> e)
		{
			if (e.Row != null)
			{
				decimal budgetedAmount = e.Row.CuryRevisedAmount.GetValueOrDefault();
				decimal result = 0;

				if (budgetedAmount != 0)
					result = e.Row.CuryPrepaymentAmount.GetValueOrDefault() * 100 / budgetedAmount;

				result = Math.Round(result, PMRevenueBudget.completedPct.Precision);

				PXFieldState fieldState = PXDecimalState.CreateInstance(result, PMRevenueBudget.completedPct.Precision, nameof(PMRevenueBudget.prepaymentPct), false, 0, Decimal.MinValue, Decimal.MaxValue);
				e.ReturnState = fieldState;
			}
		}

		protected virtual void _(Events.FieldVerifying<PMRevenueBudget, PMRevenueBudget.prepaymentPct> e)
		{
			decimal budgetedAmount = e.Row.CuryRevisedAmount.GetValueOrDefault();
			decimal? prepaymentPct = (decimal?)e.NewValue;
			decimal prepayment = Math.Max(0, (budgetedAmount * prepaymentPct.GetValueOrDefault() / 100m));

			if (prepayment < e.Row.CuryPrepaymentInvoiced)
			{
				throw new PXSetPropertyException<PMRevenueBudget.prepaymentPct>(Messages.PrepaimentLessThanInvoiced, PXErrorLevel.Error);
			}
		}

		protected virtual void _(Events.FieldUpdated<PMRevenueBudget, PMRevenueBudget.prepaymentPct> e)
		{
			if (e.Row != null)
			{
				decimal budgetedAmount = e.Row.CuryRevisedAmount.GetValueOrDefault();
				decimal prepayment = Math.Max(0, (budgetedAmount * e.Row.PrepaymentPct.GetValueOrDefault() / 100m));

				e.Cache.SetValueExt<PMRevenueBudget.curyPrepaymentAmount>(e.Row, prepayment);
			}
		}

		protected virtual void _(Events.FieldVerifying<PMRevenueBudget, PMRevenueBudget.curyPrepaymentAmount> e)
		{
			if (Project.Current?.PrepaymentEnabled == true)
			{
				decimal budgetedAmount = e.Row.CuryRevisedAmount.GetValueOrDefault();
				decimal? prepayment = (decimal?)e.NewValue;
				if (prepayment.GetValueOrDefault() < e.Row.CuryPrepaymentInvoiced)
				{
					throw new PXSetPropertyException<PMRevenueBudget.curyPrepaymentAmount>(Messages.PrepaimentLessThanInvoiced, PXErrorLevel.Error);
				}

				decimal invoicedAmount = e.Row.CuryActualAmount.GetValueOrDefault() + e.Row.CuryInvoicedAmount.GetValueOrDefault();
				if (prepayment > (budgetedAmount - invoicedAmount))
				{
					e.Cache.RaiseExceptionHandling<PMRevenueBudget.curyPrepaymentAmount>(e.Row, e.NewValue, new PXSetPropertyException<PMRevenueBudget.curyPrepaymentAmount>(Messages.PrepaymentAmointExceedsRevisedAmount, PXErrorLevel.Warning));
				}
			}
		}

		protected virtual void _(Events.FieldUpdated<PMRevenueBudget, PMRevenueBudget.curyPrepaymentAmount> e)
		{
			if (e.Row != null)
			{
				e.Row.CuryPrepaymentAvailable = e.Row.CuryPrepaymentAmount.GetValueOrDefault() - e.Row.CuryPrepaymentInvoiced.GetValueOrDefault();
				RecalculateRevenueBudget(e.Row);
			}
		}

		protected virtual void _(Events.FieldSelecting<PMRevenueBudget, PMRevenueBudget.curyAmountToInvoice> e)
		{
			if (e.Row != null && Setup.Current.AutoCompleteRevenueBudget == true && !IsRevenueGroupByTask())
			{
				decimal completedPctByCost = CalculateCompletedPercentByCost(e.Row);
				
				decimal budgetedAmount = e.Row.CuryRevisedAmount.GetValueOrDefault();
				decimal result = Math.Max(0, (budgetedAmount * completedPctByCost / 100m) - e.Row.CuryInvoicedAmount.GetValueOrDefault());
				result = Math.Round(result, 2);
				PXFieldState fieldState = PXDecimalState.CreateInstance(result, 2, nameof(PMRevenueBudget.CuryAmountToInvoice), false, 0, Decimal.MinValue, Decimal.MaxValue);
				fieldState.Enabled = false;

				e.ReturnState = fieldState;
			}
		}

		protected virtual void _(Events.FieldVerifying<PMRevenueBudget, PMRevenueBudget.curyAmountToInvoice> e)
		{
			decimal? newValue = (decimal?)e.NewValue;
			if (newValue.GetValueOrDefault() != 0)
			{
				PMTask task = PMTask.PK.FindDirty(this, e.Row.ProjectID, e.Row.ProjectTaskID);
				if (task != null && string.IsNullOrEmpty(task.BillingID))
				{
					throw new PXSetPropertyException(Messages.NoBillingRule);
				}

				if (!ContainsProgressiveBillingRule(e.Row.ProjectID, e.Row.ProjectTaskID))
				{
					e.Cache.RaiseExceptionHandling<PMRevenueBudget.curyAmountToInvoice>(e.Row, e.NewValue, new PXSetPropertyException<PMRevenueBudget.curyAmountToInvoice>(Messages.NoProgressiveRule, PXErrorLevel.Warning));
				}
			}
		}

		protected virtual void _(Events.FieldVerifying<PMRevenueBudget, PMRevenueBudget.completedPct> e)
		{
			decimal? newValue = (decimal?)e.NewValue;
			if (newValue.GetValueOrDefault() != 0)
			{
				PMTask task = PMTask.PK.FindDirty(this, e.Row.ProjectID, e.Row.ProjectTaskID);
				if (task != null && string.IsNullOrEmpty(task.BillingID))
				{
					throw new PXSetPropertyException(Messages.NoBillingRule);
				}

				if (!ContainsProgressiveBillingRule(e.Row.ProjectID, e.Row.ProjectTaskID))
				{
					e.Cache.RaiseExceptionHandling<PMRevenueBudget.completedPct>(e.Row, e.NewValue, new PXSetPropertyException<PMRevenueBudget.completedPct>(Messages.NoProgressiveRule, PXErrorLevel.Warning));
				}
			}
		}

		protected virtual void _(Events.FieldUpdated<PMRevenueBudget, PMRevenueBudget.retainagePct> e)
		{
			e.Cache.SetDefaultExt<PMRevenueBudget.curyCapAmount>(e.Row);
		}

		protected virtual void _(Events.FieldUpdated<PMRevenueBudget, PMRevenueBudget.retainageMaxPct> e)
		{
			e.Cache.SetDefaultExt<PMRevenueBudget.curyCapAmount>(e.Row);
		}

		protected virtual void _(Events.RowDeleting<PMRevenueBudget> e)
		{
			if (!BudgetEditable() && Project.Cache.GetStatus(Project.Current) != PXEntryStatus.Deleted)
				throw new PXException(Messages.BudgetLineCannotBeDeleted);
		}

		protected virtual void _(Events.RowDeleted<PMRevenueBudget> e)
		{
			var select = new PXSelect<PMCostBudget, Where<PMCostBudget.projectID, Equal<Required<PMCostBudget.projectID>>,
				And<PMCostBudget.revenueTaskID, Equal<Required<PMCostBudget.projectTaskID>>>>>(this);

			foreach (PMCostBudget budget in select.Select(e.Row.ProjectID, e.Row.ProjectTaskID))
			{
				budget.RevenueTaskID = null;
				budget.RevenueInventoryID = null;

				CostBudget.Update(budget);
			}
		}

		protected virtual void _(Events.FieldVerifying<PMRevenueBudget, PMRevenueBudget.retainagePct> e)
		{
			if (e.Row != null && e.NewValue != null)
			{
				decimal percent = (decimal)e.NewValue;
				if (percent < 0 || percent > 100)
				{
					throw new PXSetPropertyException<PMRevenueBudget.retainagePct>(IN.Messages.PercentageValueShouldBeBetween0And100);
				}

			}
		}

		#endregion

		#region Cost Budget

		protected virtual void _(Events.RowSelected<PMCostBudget> e)
		{
			if (e.Row != null)
			{
				if (CostQuantityVisible())
				{
					if ((e.Row.Qty != 0 || e.Row.RevisedQty != 0) && string.IsNullOrEmpty(e.Row.UOM))
					{
						if (string.IsNullOrEmpty(PXUIFieldAttribute.GetError<PMCostBudget.uOM>(e.Cache, e.Row)))
							PXUIFieldAttribute.SetWarning<PMCostBudget.uOM>(e.Cache, e.Row, Messages.UomNotDefinedForBudget);
					}
					else
					{
						string errorText = PXUIFieldAttribute.GetError<PMCostBudget.uOM>(e.Cache, e.Row);
						if (errorText == PXLocalizer.Localize(Messages.UomNotDefinedForBudget))
						{
							PXUIFieldAttribute.SetWarning<PMCostBudget.uOM>(e.Cache, e.Row, null);
						}
					}
				}
				else
				{
					PXUIFieldAttribute.SetWarning<PMCostBudget.uOM>(e.Cache, e.Row, null);
				}
			}			
		}

		protected virtual void _(Events.FieldUpdated<PMCostBudget, PMCostBudget.curyAmount> e)
		{
			if (e.Row != null)
			{
				e.Row.CuryRevisedAmount = e.Row.CuryAmount + e.Row.CuryChangeOrderAmount;
			}
		}

		protected virtual void _(Events.FieldUpdated<PMCostBudget, PMCostBudget.qty> e)
		{
			if (e.Row != null)
			{
				e.Row.RevisedQty = e.Row.Qty;
			}

			e.Cache.SetDefaultExt<PMCostBudget.curyUnitPrice>(e.Row);
		}

		protected virtual void _(Events.FieldUpdated<PMCostBudget, PMCostBudget.curyUnitRate> e)
		{
			if (e.Row != null)
			{
				e.Row.CuryRevisedAmount = e.Row.CuryAmount + e.Row.CuryChangeOrderAmount;
			}
		}

		protected virtual void _(Events.FieldDefaulting<PMCostBudget, PMCostBudget.curyUnitRate> e)
		{
			if (Project.Current != null)
			{
				decimal? unitCost = RateService.CalculateUnitCost(e.Cache, e.Row.ProjectID, e.Row.ProjectTaskID, e.Row.InventoryID, e.Row.UOM, null, Project.Current.StartDate, Project.Current.CuryInfoID);
				e.NewValue = unitCost ?? 0m;
			}
		}

		protected virtual void _(Events.FieldDefaulting<PMCostBudget, PMCostBudget.curyUnitPrice> e)
		{
			if (Project.Current != null)
			{
				decimal? unitPrice = RateService.CalculateUnitPrice(e.Cache, e.Row.ProjectID, e.Row.ProjectTaskID, e.Row.InventoryID, e.Row.UOM, e.Row.Qty, Project.Current.StartDate, Project.Current.CuryInfoID);
				e.NewValue = unitPrice ?? 0m;
			}
		}

		protected virtual void _(Events.FieldDefaulting<PMCostBudget, PMCostBudget.costCodeID> e)
		{
			if (Project.Current != null)
			{
				if (Project.Current.BudgetLevel != BudgetLevels.CostCode)
				{
					e.NewValue = CostCodeAttribute.GetDefaultCostCode();
				}
			}
		}

		protected virtual void _(Events.FieldUpdated<PMCostBudget, PMCostBudget.projectTaskID> e)
		{
			e.Cache.SetDefaultExt<PMCostBudget.description>(e.Row);
		}

		protected virtual void _(Events.FieldUpdated<PMCostBudget, PMCostBudget.inventoryID> e)
		{
			e.Cache.SetDefaultExt<PMCostBudget.description>(e.Row);

			if (e.Row.AccountGroupID == null)
				e.Cache.SetDefaultExt<PMCostBudget.accountGroupID>(e.Row);

			e.Cache.SetDefaultExt<PMCostBudget.uOM>(e.Row);
			e.Cache.SetDefaultExt<PMCostBudget.curyUnitRate>(e.Row);
			e.Cache.SetDefaultExt<PMCostBudget.curyUnitPrice>(e.Row);
		}

		protected virtual void _(Events.FieldUpdated<PMCostBudget, PMCostBudget.uOM> e)
		{
			e.Cache.SetDefaultExt<PMCostBudget.curyUnitRate>(e.Row);
			e.Cache.SetDefaultExt<PMCostBudget.curyUnitPrice>(e.Row);
		}

		protected virtual void _(Events.FieldUpdated<PMCostBudget, PMCostBudget.revenueTaskID> e)
		{
			var select = new PXSelect<PMRevenueBudget, Where<PMRevenueBudget.projectID, Equal<Current<PMCostBudget.projectID>>,
				And<PMRevenueBudget.projectTaskID, Equal<Current<PMCostBudget.revenueTaskID>>,
				And<PMRevenueBudget.inventoryID, Equal<Current<PMCostBudget.revenueInventoryID>>>>>>(this);

			PMRevenueBudget revenue = select.Select();

			if (revenue == null)
				e.Row.RevenueInventoryID = null;
		}

		protected virtual void _(Events.FieldDefaulting<PMCostBudget, PMCostBudget.accountGroupID> e)
		{
			if (e.Row == null) return;
			if (e.Row.InventoryID != null && e.Row.InventoryID != PMInventorySelectorAttribute.EmptyInventoryID && !IsCopyPasteContext)
			{
				InventoryItem item = PXSelectorAttribute.Select<PMCostBudget.inventoryID>(e.Cache, e.Row) as InventoryItem;
				if (item != null)
				{
					Account account = PXSelectorAttribute.Select<InventoryItem.cOGSAcctID>(Caches[typeof(InventoryItem)], item) as Account;
					if (account != null && account.AccountGroupID != null)
					{
						e.NewValue = account.AccountGroupID;
					}
				}
			}
		}

		protected virtual void _(Events.FieldUpdated<PMCostBudget, PMCostBudget.costCodeID> e)
		{
			e.Cache.SetDefaultExt<PMCostBudget.description>(e.Row);
		}

		protected virtual void _(Events.FieldDefaulting<PMCostBudget, PMCostBudget.description> e)
		{
			if (e.Row == null || Project.Current == null) return;

			if (Project.Current.CostBudgetLevel == BudgetLevels.CostCode || Project.Current.CostBudgetLevel == BudgetLevels.Detail)
			{
				if (e.Row.CostCodeID != null)
				{
					PMCostCode costCode = PXSelectorAttribute.Select<PMCostBudget.costCodeID>(e.Cache, e.Row) as PMCostCode;
					if (costCode != null)
					{
						e.NewValue = costCode.Description;
					}
				}
			}
			else if (Project.Current.CostBudgetLevel == BudgetLevels.Task)
			{
				if (e.Row.ProjectTaskID != null)
				{
					PMTask projectTask = PXSelectorAttribute.Select<PMCostBudget.projectTaskID>(e.Cache, e.Row) as PMTask;
					if (projectTask != null)
					{
						e.NewValue = projectTask.Description;
					}
				}
			}
			else if (Project.Current.CostBudgetLevel == BudgetLevels.Item)
			{
				if (e.Row.InventoryID != null && e.Row.InventoryID != PMInventorySelectorAttribute.EmptyInventoryID)
				{
					InventoryItem item = PXSelectorAttribute.Select<PMCostBudget.inventoryID>(e.Cache, e.Row) as InventoryItem;
					if (item != null)
					{
						e.NewValue = item.Descr;
					}
				}
			}
		}
			
		protected virtual void _(Events.FieldDefaulting<PMCostBudget, PMCostBudget.projectTaskID> e)
		{
			if (CostFilter.Current != null && CostFilter.Current.ProjectTaskID != null)
			{
				e.NewValue = CostFilter.Current.ProjectTaskID;
			}
		}

		protected virtual void _(Events.FieldDefaulting<PMCostBudget, PMCostBudget.inventoryID> e)
		{
			e.NewValue = PMInventorySelectorAttribute.EmptyInventoryID;
		}
				
		protected virtual void _(Events.RowDeleting<PMCostBudget> e)
		{
			if (!BudgetEditable() && Project.Cache.GetStatus(Project.Current) != PXEntryStatus.Deleted)
				throw new PXException(Messages.BudgetLineCannotBeDeleted);
		}

		protected virtual void _(Events.RowUpdated<PMCostBudget> e)
		{
			if (e.Row.ProjectTaskID != e.OldRow.ProjectTaskID ||
				e.Row.CostCodeID != e.OldRow.CostCodeID ||
				e.Row.AccountGroupID != e.OldRow.AccountGroupID ||
				e.Row.InventoryID != e.OldRow.InventoryID)
			{
				foreach (PMBudgetProduction line in BudgetProduction.Select())
				{
					if (line.ProjectTaskID == e.OldRow.ProjectTaskID &&
						line.CostCodeID == e.OldRow.CostCodeID &&
						line.AccountGroupID == e.OldRow.AccountGroupID &&
						line.InventoryID == e.OldRow.InventoryID)
					{
						PMBudgetProduction newLine = (PMBudgetProduction)BudgetProduction.Cache.CreateCopy(line);

						newLine.ProjectTaskID = e.Row.ProjectTaskID;
						newLine.CostCodeID = e.Row.CostCodeID;
						newLine.AccountGroupID = e.Row.AccountGroupID;
						newLine.InventoryID = e.Row.InventoryID;

						BudgetProduction.Delete(line);
						BudgetProduction.Insert(newLine);
					}
				}
			}
		}

		#endregion

		#region Other Budget

		protected virtual void _(Events.RowSelected<PMOtherBudget> e)
		{
			if (e.Row != null)
			{
				if ((e.Row.Qty != 0 || e.Row.RevisedQty != 0) && string.IsNullOrEmpty(e.Row.UOM))
				{
					if (string.IsNullOrEmpty(PXUIFieldAttribute.GetError<PMOtherBudget.uOM>(e.Cache, e.Row)))
						PXUIFieldAttribute.SetWarning<PMOtherBudget.uOM>(e.Cache, e.Row, Messages.UomNotDefinedForBudget);
				}
				else
				{
					string errorText = PXUIFieldAttribute.GetError<PMOtherBudget.uOM>(e.Cache, e.Row);
					if (errorText == PXLocalizer.Localize(Messages.UomNotDefinedForBudget))
					{
						PXUIFieldAttribute.SetWarning<PMOtherBudget.uOM>(e.Cache, e.Row, null);
					}
				}
			}
		}

		protected virtual void _(Events.FieldUpdated<PMOtherBudget, PMOtherBudget.costCodeID> e)
		{
			if (CostCodeAttribute.UseCostCode() && Project.Current?.BudgetLevel == BudgetLevels.CostCode)
				e.Cache.SetDefaultExt<PMOtherBudget.description>(e.Row);
		}

		protected virtual void _(Events.FieldUpdated<PMOtherBudget, PMOtherBudget.inventoryID> e)
		{
			e.Cache.SetDefaultExt<PMCostBudget.description>(e.Row);

			if (e.Row.AccountGroupID == null)
				e.Cache.SetDefaultExt<PMCostBudget.accountGroupID>(e.Row);

			e.Cache.SetDefaultExt<PMCostBudget.uOM>(e.Row);
			e.Cache.SetDefaultExt<PMCostBudget.curyUnitRate>(e.Row);
		}

		protected virtual void _(Events.FieldUpdated<PMOtherBudget, PMOtherBudget.projectTaskID> e)
		{
			e.Cache.SetDefaultExt<PMOtherBudget.description>(e.Row);
		}

		protected virtual void _(Events.FieldDefaulting<PMOtherBudget, PMOtherBudget.costCodeID> e)
		{
			if (Project.Current != null)
			{
				if (Project.Current.BudgetLevel != BudgetLevels.CostCode)
				{
					e.NewValue = CostCodeAttribute.GetDefaultCostCode();
				}
			}
		}

		protected virtual void _(Events.FieldUpdated<PMOtherBudget, PMOtherBudget.curyAmount> e)
		{
			if (e.Row != null)
			{
				e.Row.CuryRevisedAmount = e.Row.CuryAmount;
			}
		}

		protected virtual void _(Events.FieldUpdated<PMOtherBudget, PMOtherBudget.qty> e)
		{
			if (e.Row != null)
			{
				e.Row.RevisedQty = e.Row.Qty;
			}
		}
		#endregion

		#region ContractBillingSchedule Event Handlers

		protected virtual void _(Events.FieldUpdated<ContractBillingSchedule, ContractBillingSchedule.type> e)
		{
			if (e.Row != null)
			{
				if (e.Row.Type != null && Project.Current?.StartDate != null)
					e.Row.NextDate = PMBillEngine.GetNextBillingDate(this, e.Row, Project.Current.StartDate);
			}
		}

		protected virtual void _(Events.RowSelected<ContractBillingSchedule> e)
		{
			ContractBillingSchedule row = e.Row as ContractBillingSchedule;
			if (row != null)
			{
				if (Project.Current != null)
				{
					PXUIFieldAttribute.SetEnabled<ContractBillingSchedule.type>(e.Cache, row, Project.Current.CustomerID != null && Project.Current.IsActive != true);
					PXUIFieldAttribute.SetRequired<ContractBillingSchedule.type>(e.Cache, Project.Current.CustomerID != null);
					PXUIFieldAttribute.SetEnabled<ContractBillingSchedule.nextDate>(e.Cache, row, (Project.Current.IsActive == true || IsContractBasedAPI) && Project.Current.CustomerID != null);
					PXUIFieldAttribute.SetRequired<ContractBillingSchedule.nextDate>(e.Cache, true);
				}

				if (row.Type == BillingType.OnDemand)
				{
					PXUIFieldAttribute.SetEnabled<ContractBillingSchedule.nextDate>(e.Cache, row, false);
				}
			}
		}

		protected virtual void _(Events.RowPersisting<ContractBillingSchedule> e)
		{
			if (e.Operation != PXDBOperation.Delete)
			{
				ContractBillingSchedule row = e.Row as ContractBillingSchedule;
				if (row == null) return;
			}
		}
		
		#endregion

		protected virtual void _(Events.RowPersisting<PMProjectBalanceRecord> e)
		{
			e.Cancel = true;
		}

		protected virtual void _(Events.RowPersisting<PMBillingRecord> e)
		{
			if (e.Row.RecordID <= 0)
				e.Cancel = true;
		}

		protected virtual void _(Events.FieldUpdated<CostBudgetFilter, CostBudgetFilter.groupByTask> e)
		{
			if (e.Row.GroupByTask == true)
			{
				e.Row.ProjectTaskID = null;
				Project.Cache.RaiseRowSelected(Project.Current);
			}
			else
			{
				
			}

			CostBudget.Current = null;
		}
		
		protected virtual void _(Events.FieldUpdated<RevenueBudgetFilter, RevenueBudgetFilter.groupByTask> e)
		{
			if (e.Row.GroupByTask == true)
			{
				e.Row.ProjectTaskID = null;
				Project.Cache.RaiseRowSelected(Project.Current);
			}
		}

		protected virtual void _(Events.FieldUpdated<PMProject, PMProject.dropshipReceiptProcessing> e)
		{
			if (e.Row != null && (string)e.NewValue == DropshipReceiptProcessingOption.SkipReceipt)
			{
				e.Cache.SetValueExt<PMProject.dropshipExpenseRecording>(e.Row, DropshipExpenseRecordingOption.OnBillRelease);
			}
		}

		protected virtual void _(Events.FieldVerifying<PMProject, PMProject.dropshipExpenseRecording> e)
		{
			if (e.Row != null && (string)e.NewValue == DropshipExpenseRecordingOption.OnReceiptRelease && PXAccess.FeatureInstalled<FeaturesSet.inventory>())
			{
				INSetup inSetup = PXSelect<INSetup>.Select(this);
				if (inSetup?.UpdateGL != true)
				{
					throw new PXSetPropertyException<PMProject.dropshipExpenseRecording>(Messages.ProjectDropShipPostExpensesUpdateGLInactive);
				}
			}
		}

		protected virtual void _(Events.RowSelected<RevenueBudgetFilter> e)
        {
            if (e.Row != null)
            {
				if (Setup.Current.AutoCompleteRevenueBudget == true)
				{
					decimal total = 0;
					foreach(PMRevenueTotal revenue in GetAutoRevenueTotals())
					{
						total += revenue.CuryAmountToInvoiceProjected.GetValueOrDefault();
					}

					e.Row.CuryAmountToInvoiceTotal = total;
				}
				else
				{
					var select = new PXSelectGroupBy<PMRevenueBudget,
						Where<PMRevenueBudget.projectID, Equal<Current<PMProject.contractID>>,
						And<PMRevenueBudget.type, Equal<GL.AccountType.income>>>,
						Aggregate<Sum<PMRevenueBudget.curyAmountToInvoice>>>(this);

					PMRevenueBudget total = select.Select();
					if (total != null)
					{
						e.Row.CuryAmountToInvoiceTotal = total.CuryAmountToInvoice;

						foreach (PMRevenueBudget budget in RevenueBudget.Cache.Deleted)
						{
							e.Row.CuryAmountToInvoiceTotal -= budget.CuryAmountToInvoice.GetValueOrDefault();
						}

						foreach (PMRevenueBudget budget in RevenueBudget.Cache.Inserted)
						{
							e.Row.CuryAmountToInvoiceTotal += budget.CuryAmountToInvoice.GetValueOrDefault();
						}

						foreach (PMRevenueBudget budget in RevenueBudget.Cache.Updated)
						{
							decimal? originalValue = (decimal?)RevenueBudget.Cache.GetValueOriginal<PMRevenueBudget.curyAmountToInvoice>(budget);
							if (originalValue != null)
							{
								e.Row.CuryAmountToInvoiceTotal -= originalValue.Value;
							}
							e.Row.CuryAmountToInvoiceTotal += budget.CuryAmountToInvoice.GetValueOrDefault();
						}
					}
				}
            }
        }

		#region Notification Events
		protected virtual void _(Events.RowSelected<NotificationRecipient> e)
		{
			if (e.Row == null)
				return;

			Contact contact = PXSelectorAttribute.Select<NotificationRecipient.contactID>(e.Cache, e.Row) as Contact;
			if (contact != null)
			{
				e.Row.Email = contact.EMail;
			}
			else if (e.Row.ContactType == NotificationContactType.Primary)
			{
				PMContact billingContact = Billing_Contact.SelectSingle();
				if (billingContact != null)
				{
					e.Row.Email = billingContact.Email;
				}
			}
		}

		protected virtual void _(Events.RowSelected<NotificationSource> e)
		{
			if (e.Row != null)
			{
				NotificationSetup ns = PXSelect<NotificationSetup, Where<NotificationSetup.setupID, Equal<Required<NotificationSetup.setupID>>>>.Select(this, e.Row.SetupID);
				if (ns != null && (ns.NotificationCD == ProformaEntry.ProformaNotificationCD || ns.NotificationCD == ChangeOrderEntry.ChangeOrderNotificationCD))
				{
					PXUIFieldAttribute.SetEnabled<NotificationSource.active>(e.Cache, e.Row, false);
				}
				else
				{
					PXUIFieldAttribute.SetEnabled<NotificationSource.active>(e.Cache, e.Row, true);
				}
			}
		}

		protected virtual void _(Events.FieldVerifying<NotificationSource.reportID> e)
		{
			if ((string)e.NewValue == PX.Objects.CN.ProjectAccounting.PM.GraphExtensions.ProformaEntryExt.AIAReport ||
				(string)e.NewValue == PX.Objects.CN.ProjectAccounting.PM.GraphExtensions.ProformaEntryExt.AIAWithQtyReport)
			{
				throw new PXSetPropertyException(Messages.ReportsNotSupported, PX.Objects.CN.ProjectAccounting.PM.GraphExtensions.ProformaEntryExt.AIAReport, PX.Objects.CN.ProjectAccounting.PM.GraphExtensions.ProformaEntryExt.AIAWithQtyReport);
			}
		}
		#endregion

		protected virtual void _(Events.FieldVerifying<CopyDialogInfo, CopyDialogInfo.projectID> e)
		{
			var select = new PXSelect<PMProject, Where<PMProject.contractCD, Equal<Required<PMProject.contractCD>>,
				And<PMProject.baseType, Equal<CTPRType.project>>>>(this);

			PMProject duplicate = select.Select(e.NewValue);

			if (duplicate != null)
			{
				throw new PXSetPropertyException<CopyDialogInfo.projectID>(Messages.DuplicateProjectCD);
			}
		}

		public virtual void _(Events.FieldUpdated<PMSiteAddress, PMSiteAddress.countryID> args)
		{
			if (args.Row == null) return;
			args.Row.State = null;
		}
		#endregion

		/// <summary>
		/// Creates a new instance of ProjectEntry graph and inserts copies of entities from current graph.
		/// Redirects to target graph on completion.
		/// </summary>
		public virtual void Copy(PMProject project)
		{
			bool isAutonumbered = DimensionMaint.IsAutonumbered(this, ProjectAttribute.DimensionName);

			string newContractCD = null;
			if (!isAutonumbered)
			{
				if (CopyDialog.AskExt() == WebDialogResult.Yes && !string.IsNullOrEmpty(CopyDialog.Current.ProjectID))
				{
					newContractCD = CopyDialog.Current.ProjectID;
				}
				else
				{
					return;
				}
			}

			ProjectEntry target = PXGraph.CreateInstance<ProjectEntry>();
			target.SelectTimeStamp();
			target.IsCopyPaste = true;
			target.CopySource = this;

			PMProject newProject = (PMProject)Project.Cache.CreateCopy(project);
			newProject.ContractID = null;
			newProject.ContractCD = newContractCD;
			newProject.Status = null;
			newProject.Hold = null;
			newProject.StartDate = null;
			newProject.ExpireDate = null;
			newProject.BudgetFinalized = null;
			newProject.LastChangeOrderNumber = null;
			newProject.LastProformaNumber = null;
			newProject.IsActive = null;
			newProject.IsCompleted = null;
			newProject.IsCancelled = null;
			newProject.NoteID = null;
			newProject.CuryInfoID = null;
			newProject.BaseCuryID = null;
			newProject = target.Project.Insert(newProject);

			target.Billing.Cache.Clear();
			ContractBillingSchedule schedule = (ContractBillingSchedule)Billing.Cache.CreateCopy(Billing.SelectSingle());
			schedule.ContractID = newProject.ContractID;
			schedule.LastDate = null;
			target.Billing.Insert(schedule);

			target.FieldDefaulting.AddHandler<PMTask.billingID>((PXCache cache, PXFieldDefaultingEventArgs e) => { e.Cancel = true; });
			target.FieldDefaulting.AddHandler<PMTask.allocationID>((PXCache cache, PXFieldDefaultingEventArgs e) => { e.Cancel = true; });
			target.FieldDefaulting.AddHandler<PMTask.rateTableID>((PXCache cache, PXFieldDefaultingEventArgs e) => { e.Cancel = true; });

			Dictionary<int, int> taskMap = new Dictionary<int, int>();
			foreach (PMTask task in Tasks.Select())
			{
				PMTask newTask = (PMTask)Tasks.Cache.CreateCopy(task);
				newTask.TaskID = null;
				newTask.ProjectID = newProject.ContractID;
				newTask.IsActive = null;
				newTask.IsCompleted = null;
				newTask.IsCancelled = null;
				newTask.Status = null;
				newTask.StartDate = null;
				newTask.EndDate = null;
				newTask.PlannedStartDate = null;
				newTask.PlannedEndDate = null;
				newTask.CompletedPercent = null;
				newTask.NoteID = null;

				if (task.PlannedStartDate != null && Accessinfo.BusinessDate != null && project.StartDate != null)
				{
					newTask.PlannedStartDate = this.Accessinfo.BusinessDate.Value.AddDays(task.PlannedStartDate.Value.Subtract(project.StartDate.Value).TotalDays);
					if(task.PlannedEndDate != null)
					{
						newTask.PlannedEndDate = newTask.PlannedStartDate.Value.AddDays(task.PlannedEndDate.Value.Subtract(task.PlannedStartDate.Value).TotalDays);
					}
				}

				newTask = target.Tasks.Insert(newTask);
				taskMap.Add(task.TaskID.Value, newTask.TaskID.Value);

				target.TaskAnswers.CopyAllAttributes(newTask, task);
			}

			OnCopyPasteTasksInserted(target, taskMap);

			foreach (PMRevenueBudget budget in RevenueBudget.Select())
			{
				PMRevenueBudget newBudget = (PMRevenueBudget)RevenueBudget.Cache.CreateCopy(budget);
				newBudget.ProjectID = newProject.ContractID;
				newBudget.ProjectTaskID = taskMap[budget.TaskID.Value];
				newBudget.CuryActualAmount = 0;
				newBudget.ActualAmount = 0;
				newBudget.ActualQty = 0;
				newBudget.QtyToInvoice = 0;
				newBudget.CuryAmountToInvoice = 0;
				newBudget.AmountToInvoice = 0;
				newBudget.CuryDraftChangeOrderAmount = 0;
				newBudget.DraftChangeOrderAmount = 0;
				newBudget.DraftChangeOrderQty = 0;
				newBudget.CuryChangeOrderAmount = 0;
				newBudget.ChangeOrderAmount = 0;
				newBudget.ChangeOrderQty = 0;
				newBudget.CuryCommittedOrigAmount = 0;
				newBudget.CommittedOrigAmount = 0;
				newBudget.CommittedOrigQty = 0;
				newBudget.CuryCommittedAmount = 0;
				newBudget.CommittedAmount = 0;
				newBudget.CuryCommittedInvoicedAmount = 0;
				newBudget.CommittedInvoicedAmount = 0;
				newBudget.CommittedInvoicedQty = 0;
				newBudget.CuryCommittedOpenAmount = 0;
				newBudget.CommittedOpenAmount = 0;
				newBudget.CommittedOpenQty = 0;
				newBudget.CommittedQty = 0;
				newBudget.CommittedReceivedQty = 0;
				newBudget.CompletedPct = 0;
				newBudget.CuryCostAtCompletion = 0;
				newBudget.CostAtCompletion = 0;
				newBudget.CuryCostToComplete = 0;
				newBudget.CostToComplete = 0;
				newBudget.CuryInvoicedAmount = 0;
				newBudget.InvoicedAmount = 0;
				newBudget.InvoicedQty = 0;
				newBudget.CuryLastCostAtCompletion = 0;
				newBudget.LastCostAtCompletion = 0;
				newBudget.CuryLastCostToComplete = 0;
				newBudget.LastCostToComplete = 0;
				newBudget.LastPercentCompleted = 0;
				newBudget.PercentCompleted = 0;
				newBudget.CuryPrepaymentInvoiced = 0;
				newBudget.PrepaymentInvoiced = 0;
				newBudget.CuryDraftRetainedAmount = 0;
				newBudget.DraftRetainedAmount = 0;
				newBudget.CuryRetainedAmount = 0;
				newBudget.RetainedAmount = 0;
				newBudget.CuryTotalRetainedAmount = 0;
				newBudget.TotalRetainedAmount = 0;
				newBudget.LineCntr = null;
				newBudget.NoteID = null;
				newBudget.CuryInfoID = newProject.CuryInfoID;
				if (project.ChangeOrderWorkflow == true)
				{
					newBudget.RevisedQty = null;
					newBudget.RevisedAmount = null;
					newBudget.CuryRevisedAmount = null;
				}

				newBudget = target.RevenueBudget.Insert(newBudget);
				target.RevenueBudget.Cache.SetValueExt<PMRevenueBudget.progressBillingBase>(newBudget, budget.ProgressBillingBase);
			}

			foreach (PMCostBudget budget in CostBudget.Select())
			{
				PMCostBudget newBudget = (PMCostBudget)CostBudget.Cache.CreateCopy(budget);
				newBudget.ProjectID = newProject.ContractID;
				newBudget.ProjectTaskID = taskMap[budget.TaskID.Value];
				newBudget.CuryActualAmount = 0;
				newBudget.ActualAmount = 0;
				newBudget.ActualQty = 0;
				newBudget.CuryAmountToInvoice = 0;
				newBudget.QtyToInvoice = 0;
				newBudget.AmountToInvoice = 0;
				newBudget.CuryDraftChangeOrderAmount = 0;
				newBudget.DraftChangeOrderAmount = 0;
				newBudget.DraftChangeOrderQty = 0;
				newBudget.CuryChangeOrderAmount = 0;
				newBudget.ChangeOrderAmount = 0;
				newBudget.ChangeOrderQty = 0;
				newBudget.CuryCommittedOrigAmount = 0;
				newBudget.CommittedOrigAmount = 0;
				newBudget.CommittedOrigQty = 0;
				newBudget.CuryCommittedAmount = 0;
				newBudget.CommittedAmount = 0;
				newBudget.CuryCommittedInvoicedAmount = 0;
				newBudget.CommittedInvoicedAmount = 0;
				newBudget.CommittedInvoicedQty = 0;
				newBudget.CuryCommittedOpenAmount = 0;
				newBudget.CommittedOpenAmount = 0;
				newBudget.CommittedOpenQty = 0;
				newBudget.CommittedQty = 0;
				newBudget.CommittedReceivedQty = 0;
				newBudget.CompletedPct = 0;
				newBudget.CuryCostAtCompletion = 0;
				newBudget.CostAtCompletion = 0;
				newBudget.CuryCostToComplete = 0;
				newBudget.CostToComplete = 0;
				newBudget.CuryInvoicedAmount = 0;
				newBudget.InvoicedAmount = 0;
				newBudget.CuryLastCostAtCompletion = 0;
				newBudget.LastCostAtCompletion = 0;
				newBudget.CuryLastCostToComplete = 0;
				newBudget.LastCostToComplete = 0;
				newBudget.LastPercentCompleted = 0;
				newBudget.PercentCompleted = 0;
				newBudget.CuryPrepaymentInvoiced = 0;
				newBudget.PrepaymentInvoiced = 0;
				newBudget.CuryDraftRetainedAmount = 0;
				newBudget.DraftRetainedAmount = 0;
				newBudget.CuryRetainedAmount = 0;
				newBudget.RetainedAmount = 0;
				newBudget.CuryTotalRetainedAmount = 0;
				newBudget.TotalRetainedAmount = 0;
				newBudget.LineCntr = null;
				newBudget.NoteID = null;
				newBudget.RevenueTaskID = budget.RevenueTaskID == null ? null : ((int?)taskMap[budget.RevenueTaskID.Value]);
				newBudget.CuryInfoID = newProject.CuryInfoID;
				if (project.ChangeOrderWorkflow == true)
				{
					newBudget.RevisedQty = null;
					newBudget.RevisedAmount = null;
					newBudget.CuryRevisedAmount = null;
				}

				target.CostBudget.Insert(newBudget);
			}

			target.Project.Cache.SetValueExt<PMProject.baseCuryID>(newProject, project.BaseCuryID);
			target.Project.Cache.SetValueExt<PMProject.curyID>(newProject, project.CuryID);
			target.Project.Cache.SetValueExt<PMProject.curyIDCopy>(newProject, project.CuryIDCopy);

			var userDefinedFieldValues = Project.Cache.Fields
			   .Where(Project.Cache.IsKvExtAttribute)
			   .ToDictionary(
				   udField => udField,
				   udField => ((PXFieldState)Project.Cache.GetValueExt(project, udField))?.Value);

			foreach ((string fieldName, object value) in userDefinedFieldValues)
			{
				target.Project.Cache.SetValueExt(newProject, fieldName, value);
			}

			foreach (EPEmployeeContract employee in EmployeeContract.Select())
			{
				EPEmployeeContract newEmployee = (EPEmployeeContract)EmployeeContract.Cache.CreateCopy(employee);
				newEmployee.ContractID = newProject.ContractID;
				target.EmployeeContract.Insert(newEmployee);
			}

			foreach (EPContractRate rate in ContractRates.Select())
			{
				EPContractRate newRate = (EPContractRate)ContractRates.Cache.CreateCopy(rate);
				newRate.ContractID = newProject.ContractID;
				target.ContractRates.Insert(newRate);
			}

			foreach (EPEquipmentRate rate in EquipmentRates.Select())
			{
				EPEquipmentRate newRate = (EPEquipmentRate)EquipmentRates.Cache.CreateCopy(rate);
				newRate.ProjectID = newProject.ContractID;
				newRate.NoteID = null;
				target.EquipmentRates.Insert(newRate);
			}

			foreach (PMAccountTask account in Accounts.Select())
			{
				PMAccountTask newAccount = (PMAccountTask)Accounts.Cache.CreateCopy(account);
				newAccount.ProjectID = newProject.ContractID;
				newAccount.TaskID = taskMap[account.TaskID.Value];
				newAccount.NoteID = null;
				target.Accounts.Insert(newAccount);
			}

			foreach (PMRetainageStep step in RetainageSteps.Select())
			{
				step.ProjectID = target.Project.Current.ContractID;
				step.NoteID = null;
				target.Caches[typeof(PMRetainageStep)].Insert(step);
			}

			target.NotificationSources.Cache.Clear();
			target.NotificationRecipients.Cache.Clear();

			foreach (NotificationSource source in NotificationSources.Select())
			{
				int? sourceID = source.SourceID;
				source.SourceID = null;
				source.RefNoteID = null;
				NotificationSource newsource = target.NotificationSources.Insert(source);

				foreach (NotificationRecipient recipient in NotificationRecipients.Select(sourceID))
				{
					if (recipient.ContactType == NotificationContactType.Primary || recipient.ContactType == NotificationContactType.Employee)
					{
						recipient.NotificationID = null;
						recipient.SourceID = newsource.SourceID;
						recipient.RefNoteID = null;

						target.NotificationRecipients.Insert(recipient);
					}
				}
			}

			target.Views.Caches.Add(typeof(PMRecurringItem));

			foreach (PMRecurringItem detail in PXSelect<PMRecurringItem, Where<PMRecurringItem.projectID, Equal<Required<PMRecurringItem.projectID>>>>.Select(this, project.ContractID))
			{
				PMRecurringItem newDetail = (PMRecurringItem)this.Caches[typeof(PMRecurringItem)].CreateCopy(detail);
				newDetail.ProjectID = newProject.ContractID;
				newDetail.TaskID = taskMap[detail.TaskID.Value];
				newDetail.Used = null;
				newDetail.LastBilledDate = null;
				newDetail.LastBilledQty = null;
				newDetail.NoteID = null;

				target.Caches[typeof(PMRecurringItem)].Insert(newDetail);
			}

			target.Answers.CopyAllAttributes(newProject, project);


			PXRedirectHelper.TryRedirect(target, PXRedirectHelper.WindowMode.Same);
		}

		protected virtual void OnCopyPasteTasksInserted(ProjectEntry target, Dictionary<int, int> taskMap)
		{
			//thi method is used to extend Copy in Customizations.
		}

		protected virtual void OnDefaultFromTemplateTasksInserted(PMProject project, PMProject template, Dictionary<int, int> taskMap)
		{
			//thi method is used to extend DefaultFromTemplate in Customizations.
		}

		protected virtual void OnCreateTemplateTasksInserted(TemplateMaint target, PMProject template, Dictionary<int, int> taskMap)
		{
			//thi method is used to extend CreateTemplate in Customizations.
		}


		/// <summary>
		/// Returns true both for source as well as target graph during copy-paste procedure. 
		/// </summary>
		public bool IsCopyPaste
		{
			get;
			private set;
		}

		/// <summary>
		/// During Paste of Copied Project this propert holds the reference to the Graph with source data.
		/// </summary>
		public ProjectEntry CopySource
		{
			get;
			private set;
		}

		public virtual bool BudgetEditable()
		{
			if (Project.Current != null)
			{
				return Project.Current.BudgetFinalized != true;
			}

			return false;
		}

		public virtual bool RevisedEditable()
		{
			if (Project.Current != null)
			{
				return Project.Current.ChangeOrderWorkflow != true;
			}

			return true;
		}


		public virtual bool ChangeOrderVisible()
		{
			if (Project.Current != null)
			{
				return Project.Current.ChangeOrderWorkflow == true;
			}

			return PXAccess.FeatureInstalled<FeaturesSet.changeOrder>();
		}

		public virtual bool PrepaymentVisible()
		{
			if (Project.Current != null)
			{
				return Project.Current.PrepaymentEnabled == true;
			}

			return false;
		}

		public virtual bool LimitsVisible()
		{
			if (Project.Current != null)
			{
				return Project.Current.LimitsEnabled == true;
			}

			return false;
		}

		public virtual bool ProductivityVisible()
		{
			if (Project.Current != null)
			{
				return Project.Current.BudgetMetricsEnabled == true;
			}

			return false;
		}

		public virtual bool CostQuantityVisible()
		{
			if (IsCostGroupByTask())
				return false;
					
			return true;
		}

		public virtual bool RevenueQuantityVisible()
		{
			if (IsRevenueGroupByTask())
				return false;

			return true;
		}

		public virtual bool CostBudgetIsEditable()
		{
			return !IsCostGroupByTask();
		}

		public virtual bool RevenueBudgetIsEditable()
		{
			return !IsRevenueGroupByTask();
		}

		public virtual bool IsCostGroupByTask()
		{
			if (CostFilter.Current != null && CostFilter.Current.GroupByTask == true)
				return true;

			return false;
		}

		public virtual bool IsRevenueGroupByTask()
		{
			if (RevenueFilter.Current != null && RevenueFilter.Current.GroupByTask == true)
				return true;

			return false;
		}
		
		public virtual PMTask CopyTask(PMTask task, int ProjectID, DefaultFromTemplateSettings settings)
		{
			task = PXSelect<PMTask, Where<PMTask.projectID, Equal<Current<PMTask.projectID>>, And<PMTask.taskCD, Equal<Current<PMTask.taskCD>>>>>.SelectSingleBound(this, new object[] { task });
			PMTask dst = CopyTask(task, ProjectID);

			if (settings.CopyAttributes)
			{
				TaskAnswers.CopyAllAttributes(dst, task);

			}

			if (settings.CopyBudget)
			{

			var selectCostBudget = new PXSelect<PMCostBudget,
				Where<PMCostBudget.projectID, Equal<Required<PMCostBudget.projectID>>,
				And<PMCostBudget.projectTaskID, Equal<Required<PMCostBudget.projectTaskID>>,
				And<PMCostBudget.type, Equal<GL.AccountType.expense>>>>>(this);

			foreach (PMCostBudget budget in selectCostBudget.Select(task.ProjectID, task.TaskID))
			{
				//Note: CostBudgets With RevenueTaskID are added in the calling method - as they can be added only after all tasks and revenue budgets are already in the system to reference them.

				if (budget.RevenueTaskID == null)
				{
					budget.ProjectID = Project.Current.ContractID;
					budget.ProjectTaskID = dst.TaskID;
					budget.RevisedQty = budget.Qty;
					budget.CuryRevisedAmount = budget.CuryAmount;
					budget.NoteID = null;
					CostBudget.Insert(budget);
				}
			}

			var selectRevenueBudget = new PXSelect<PMRevenueBudget,
				Where<PMRevenueBudget.projectID, Equal<Required<PMRevenueBudget.projectID>>,
				And<PMRevenueBudget.projectTaskID, Equal<Required<PMRevenueBudget.projectTaskID>>,
				And<PMRevenueBudget.type, Equal<GL.AccountType.income>>>>>(this);

			if (Project.Current.BudgetLevel != BudgetLevels.Task)
			{
				foreach (PMRevenueBudget budget in selectRevenueBudget.Select(task.ProjectID, task.TaskID))
				{
					budget.ProjectID = Project.Current.ContractID;
					budget.ProjectTaskID = dst.TaskID;
					budget.RevisedQty = budget.Qty;
					budget.CuryRevisedAmount = budget.CuryAmount;
					budget.NoteID = null;
					RevenueBudget.Insert(budget);
				}
			}
			else
			{
				//Common Task may contain multiple records split by InventoryID/CostCodeID but we have to import the aggregates for <N/A>/000000

				Dictionary<string, PMRevenueBudget> aggregated = new Dictionary<string, PMRevenueBudget>();

				foreach (PMRevenueBudget budget in selectRevenueBudget.Select(task.ProjectID, task.TaskID))
				{
					string key = string.Format("{0}.{1}", budget.ProjectTaskID, budget.AccountGroupID);

					PMRevenueBudget summary = null;
					if (!aggregated.TryGetValue(key, out summary))
					{
						budget.InventoryID = PMInventorySelectorAttribute.EmptyInventoryID;
						budget.CostCodeID = CostCodeAttribute.GetDefaultCostCode();

						aggregated.Add(key, budget);
					}
					else
					{
						if (budget.UOM != summary.UOM || string.IsNullOrEmpty(summary.UOM))
						{
							summary.UOM = null;
							summary.Qty = 0;
							summary.RevisedQty = 0;
						}

						if (!string.IsNullOrEmpty(summary.UOM))
						{
							summary.Qty += budget.Qty.GetValueOrDefault();
							summary.RevisedQty += budget.Qty.GetValueOrDefault();
						}
							summary.CuryAmount += budget.CuryAmount.GetValueOrDefault();
							summary.CuryRevisedAmount += budget.CuryAmount.GetValueOrDefault();
					}
				}

				foreach (PMRevenueBudget budget in aggregated.Values)
				{
					budget.ProjectID = Project.Current.ContractID;
					budget.ProjectTaskID = dst.TaskID;
					budget.RevisedQty = budget.Qty;
						budget.CuryRevisedAmount = budget.CuryAmount;
					budget.NoteID = null;
						string progressBillingBase = budget.ProgressBillingBase;
					var inserted = RevenueBudget.Insert(budget);
						inserted.ProgressBillingBase = progressBillingBase;
				}

			}

			}

			if (settings.CopyRecurring)
			{
				foreach (PMRecurringItem detail in PXSelect<PMRecurringItem, Where<PMRecurringItem.projectID, Equal<Current<PMTask.projectID>>, And<PMRecurringItem.taskID, Equal<Current<PMTask.taskID>>>>>.SelectMultiBound(this, new object[] { task }))
			{
					PMRecurringItem newDetail = new PMRecurringItem();
					newDetail.ProjectID = ProjectID;
				newDetail.TaskID = dst.TaskID;
				newDetail.InventoryID = detail.InventoryID;
				newDetail.UOM = detail.UOM;
				newDetail.Description = detail.Description;
					newDetail.Amount = detail.Amount;
					newDetail.AccountSource = detail.AccountSource;
					newDetail.SubMask = detail.SubMask;
					newDetail.AccountID = detail.AccountID;
					newDetail.SubID = detail.SubID;
				newDetail.BranchID = detail.BranchID;
					newDetail.ResetUsage = detail.ResetUsage;
					newDetail.Included = detail.Included;

				BillingItems.Insert(newDetail);
			}
			}

			return dst;
		}

		private PMTask CopyTask(PMTask task, int ProjectID)
		{
			PMTask dst = Tasks.Insert(new PMTask { TaskCD = task.TaskCD, ProjectID = ProjectID });
			dst.RateTableID = task.RateTableID ?? Project.Current.RateTableID;
			dst.AllocationID = task.AllocationID ?? Project.Current.AllocationID;
			dst.BillingID = task.BillingID ?? Project.Current.BillingID;
			dst.Description = task.Description;
			PXDBLocalizableStringAttribute.CopyTranslations<PMTask.description, PMTask.description>(Tasks.Cache, task, Tasks.Cache, dst);
			dst.ApproverID = task.ApproverID;
			dst.TaxCategoryID = task.TaxCategoryID;
			dst.BillingOption = task.BillingOption;
			dst.DefaultSalesAccountID = task.DefaultSalesAccountID ?? Project.Current.DefaultSalesAccountID;
			dst.DefaultSalesSubID = task.DefaultSalesSubID ?? Project.Current.DefaultSalesSubID;
			dst.DefaultExpenseAccountID = task.DefaultExpenseAccountID ?? Project.Current.DefaultExpenseAccountID;
			dst.DefaultExpenseSubID = task.DefaultExpenseSubID ?? Project.Current.DefaultExpenseSubID;
			dst.DefaultAccrualAccountID = task.DefaultAccrualAccountID ?? Project.Current.DefaultAccrualAccountID;
			dst.DefaultAccrualSubID = task.DefaultAccrualSubID ?? Project.Current.DefaultAccrualSubID;
			dst.DefaultBranchID = task.DefaultBranchID ?? Project.Current.DefaultBranchID;
			dst.CompletedPctMethod = task.CompletedPctMethod;
			dst.BillSeparately = task.BillSeparately;
			dst.WipAccountGroupID = task.WipAccountGroupID;
			dst.TaxCategoryID = task.TaxCategoryID;
			dst.VisibleInGL = task.VisibleInGL;
			dst.VisibleInAP = task.VisibleInAP;
			dst.VisibleInAR = task.VisibleInAR;
			dst.VisibleInSO = task.VisibleInSO;
			dst.VisibleInPO = task.VisibleInPO;
			dst.VisibleInTA = task.VisibleInTA;
			dst.VisibleInEA = task.VisibleInEA;
			dst.VisibleInIN = task.VisibleInIN;
			dst.VisibleInCA = task.VisibleInCA;
			dst.VisibleInCR = task.VisibleInCR;
			dst.IsActive = task.IsActive ?? false;
			dst.TemplateID = task.TaskID;
			dst.IsDefault = task.IsDefault;
			dst.ProgressBillingBase = task.ProgressBillingBase;
			dst.Type = task.Type;
			return dst;
		}

		public virtual void DefaultFromTemplate(PMProject prj, int? templateID, DefaultFromTemplateSettings settings)
		{
			PMProject templ = PMProject.PK.Find(this, templateID);
			if (templ == null) return;

			if (settings.CopyProperties && _isLoadFromTemplate == false)
				DefaultFromTemplateProjectSettings(prj, templ);

			if (settings.CopyAttributes)
			{
				Answers.CopyAllAttributes(prj, templ);
				foreach (CSAnswers answer in Answers.Cache.Inserted)
				{
					if (answer.RefNoteID == templ.NoteID)
						Answers.Delete(answer);
				}
			}

			prj.StartDate = Accessinfo.BusinessDate;

			if (settings.CopyCurrency)
				Project.Cache.SetDefaultExt<PMProject.baseCuryID>(prj);

			Dictionary<string, PMTask> srcTasksByTaskCD = PXSelect<PMTask, Where<PMTask.projectID, Equal<Current<PMProject.contractID>>>>
				.SelectMultiBound(this, new object[] { templ })
				.RowCast<PMTask>()
				.ToDictionary(t => t.TaskCD, PXLocalesProvider.CollationComparer);
			Dictionary<int, PMTask> srcTasksByTaskID = srcTasksByTaskCD.Values.ToDictionary(t => t.TaskID.Value);
			if (settings.CopyTasks)
			{
				foreach (PMTask srcTask in srcTasksByTaskCD.Values)
				{
					if (srcTask.AutoIncludeInPrj == true)
					{
						CopyTask(srcTask, prj.ContractID.Value);
					}
				}

				if (_isLoadFromTemplate)
				{
					this.Save.Press();
				}
			}

			Dictionary<string, PMTask> destTasks = PXSelect<PMTask, Where<PMTask.projectID, Equal<Current<PMProject.contractID>>>>
				.SelectMultiBound(this, new object[] { Project.Current })
				.RowCast<PMTask>()
				.ToDictionary(t => t.TaskCD, PXLocalesProvider.CollationComparer);

			Dictionary<int, int> taskIds = new Dictionary<int, int>();
			if (settings.CopyTasks)
			{
				Dictionary<string, List<CSAnswers>> taskAnswersByTaskCD = new Dictionary<string, List<CSAnswers>>(PXLocalesProvider.CollationComparer);
				foreach (PXResult<CSAnswers, PMTask> item in PXSelectJoin<CSAnswers, InnerJoin<PMTask, On<CSAnswers.refNoteID, Equal<PMTask.noteID>>>,
					Where<PMTask.projectID, Equal<Required<PMTask.projectID>>>>.Select(this, templ.ContractID))
			{
					PMTask task = item;
					CSAnswers answer = item;

					List<CSAnswers> taskAnswersList;
					if (!taskAnswersByTaskCD.TryGetValue(task.TaskCD, out taskAnswersList))
				{
						taskAnswersList = new List<CSAnswers>();
						taskAnswersByTaskCD.Add(task.TaskCD, taskAnswersList);
				}
					taskAnswersList.Add(answer);
			}

				foreach (PMTask destTask in destTasks.Values)
				{
					PMTask srcTask;
					if (srcTasksByTaskCD.TryGetValue(destTask.TaskCD, out srcTask))
					{
						taskIds.Add(srcTask.TaskID.Value, destTask.TaskID.Value);

						if (srcTask.AutoIncludeInPrj == true)
						{
							if (settings.CopyAttributes && taskAnswersByTaskCD.TryGetValue(destTask.TaskCD, out var taskAnswersList) && taskAnswersList.Count > 0)
							{
								TaskAnswers.CopyAllAttributes(destTask, srcTask);
							}
						}
					}
			}

			if (settings.CopyBudget)
			{
					Dictionary<string, List<PMRevenueBudget>> revenueBudgetsByTaskCD = new Dictionary<string, List<PMRevenueBudget>>(PXLocalesProvider.CollationComparer);

					foreach (PMRevenueBudget budget in PXSelect<PMRevenueBudget, Where<PMRevenueBudget.projectID, Equal<Required<PMRevenueBudget.projectID>>,
						And<PMRevenueBudget.type, Equal<GL.AccountType.income>>>>.Select(this, templ.ContractID))
			{
						PMTask task;
						if (srcTasksByTaskID.TryGetValue(budget.ProjectTaskID.Value, out task))
				{
							List<PMRevenueBudget> taskBudgets;
							if (!revenueBudgetsByTaskCD.TryGetValue(task.TaskCD, out taskBudgets))
							{
								taskBudgets = new List<PMRevenueBudget>();
								revenueBudgetsByTaskCD.Add(task.TaskCD, taskBudgets);
							}
							taskBudgets.Add(budget);
						}
					}

					if (Project.Current.BudgetLevel != BudgetLevels.Task)
					{
						foreach (KeyValuePair<string, List<PMRevenueBudget>> taskBudgets in revenueBudgetsByTaskCD)
						{
							PMTask destTask;
							if (destTasks.TryGetValue(taskBudgets.Key, out destTask))
							{
								foreach (PMRevenueBudget budget in taskBudgets.Value)
								{
									budget.ProjectID = Project.Current.ContractID;
									budget.ProjectTaskID = destTask.TaskID;
									budget.RevisedQty = budget.Qty;
									budget.CuryRevisedAmount = budget.CuryAmount;
									budget.NoteID = null;
									RevenueBudget.Insert(budget);
								}
							}
						}
					}
					else
					{
						//Common Task may contain multiple records split by InventoryID/CostCodeID but we have to import the aggregates for <N/A>/000000

						foreach (KeyValuePair<string, List<PMRevenueBudget>> taskBudgets in revenueBudgetsByTaskCD)
						{
							PMTask destTask;
							if (destTasks.TryGetValue(taskBudgets.Key, out destTask))
							{
								Dictionary<string, PMRevenueBudget> aggregated = new Dictionary<string, PMRevenueBudget>();

								foreach (PMRevenueBudget budget in taskBudgets.Value)
								{
									string key = string.Format("{0}.{1}", budget.ProjectTaskID, budget.AccountGroupID);

									PMRevenueBudget summary = null;
									if (!aggregated.TryGetValue(key, out summary))
									{
										budget.InventoryID = PMInventorySelectorAttribute.EmptyInventoryID;
										budget.CostCodeID = CostCodeAttribute.GetDefaultCostCode();

										aggregated.Add(key, budget);
									}
									else
									{
										if (budget.UOM != summary.UOM || string.IsNullOrEmpty(summary.UOM))
										{
											summary.UOM = null;
											summary.Qty = 0;
											summary.RevisedQty = 0;
										}

										if (!string.IsNullOrEmpty(summary.UOM))
										{
											summary.Qty += budget.Qty.GetValueOrDefault();
											summary.RevisedQty += budget.Qty.GetValueOrDefault();
										}
										summary.CuryAmount += budget.CuryAmount.GetValueOrDefault();
										summary.CuryRevisedAmount += budget.CuryAmount.GetValueOrDefault();
									}
								}

								foreach (PMRevenueBudget budget in aggregated.Values)
								{
									budget.ProjectID = Project.Current.ContractID;
									budget.ProjectTaskID = destTask.TaskID;
									budget.RevisedQty = budget.Qty;
									budget.CuryRevisedAmount = budget.CuryAmount;
									budget.NoteID = null;
									RevenueBudget.Insert(budget);
								}
							}
						}
					}

					Dictionary<string, List<PMCostBudget>> costBudgetsByTaskCD = new Dictionary<string, List<PMCostBudget>>(PXLocalesProvider.CollationComparer);

					foreach (PMCostBudget budget in PXSelect<PMCostBudget,
						Where<PMCostBudget.projectID, Equal<Required<PMCostBudget.projectID>>,
						And<PMCostBudget.type, Equal<GL.AccountType.expense>>>>.Select(this, templ.ContractID))
					{
						PMTask task;
						if (srcTasksByTaskID.TryGetValue(budget.ProjectTaskID.Value, out task))
						{
							List<PMCostBudget> taskBudgets;
							if (!costBudgetsByTaskCD.TryGetValue(task.TaskCD, out taskBudgets))
							{
								taskBudgets = new List<PMCostBudget>();
								costBudgetsByTaskCD.Add(task.TaskCD, taskBudgets);
							}
							taskBudgets.Add(budget);
						}
					}

					foreach (KeyValuePair<string, List<PMCostBudget>> taskBudgets in costBudgetsByTaskCD)
					{
						PMTask destTask;
						if (destTasks.TryGetValue(taskBudgets.Key, out destTask))
						{
							foreach (PMCostBudget budget in taskBudgets.Value)
						{
							budget.ProjectID = Project.Current.ContractID;
								budget.ProjectTaskID = destTask.TaskID;
							budget.RevisedQty = budget.Qty;
								budget.CuryRevisedAmount = budget.CuryAmount;
								budget.NoteID = null;

							int? revenueInventoryIDPending = null;
								PMTask destRevenueTask;
								PMTask srcRevenueTask;
								if (budget.RevenueTaskID != null &&
									srcTasksByTaskID.TryGetValue(budget.RevenueTaskID.Value, out srcRevenueTask) &&
									destTasks.TryGetValue(srcRevenueTask.TaskCD, out destRevenueTask))
							{
									budget.RevenueTaskID = destRevenueTask.TaskID;
								revenueInventoryIDPending = budget.RevenueInventoryID;
								budget.RevenueInventoryID = null;
							}
							else
							{
								budget.RevenueTaskID = null;
								budget.RevenueInventoryID = null;
							}

							var newBudget = CostBudget.Insert(budget);
							if (revenueInventoryIDPending != null)
							{
								CostBudget.Cache.SetValue<PMCostBudget.revenueInventoryID>(newBudget, revenueInventoryIDPending);
							}
						}
					}
				}
			}

				if (settings.CopyRecurring)
				{
					Dictionary<string, List<PMRecurringItem>> srcRecurringItemsByTaskCD = new Dictionary<string, List<PMRecurringItem>>(PXLocalesProvider.CollationComparer);

					foreach (PMRecurringItem item in PXSelect<PMRecurringItem, Where<PMRecurringItem.projectID, Equal<Required<PMRecurringItem.projectID>>>>.Select(this, templ.ContractID))
					{
						PMTask task;
						if (srcTasksByTaskID.TryGetValue(item.TaskID.Value, out task))
						{
							List<PMRecurringItem> taskRecurringItems;
							if (!srcRecurringItemsByTaskCD.TryGetValue(task.TaskCD, out taskRecurringItems))
							{
								taskRecurringItems = new List<PMRecurringItem>();
								srcRecurringItemsByTaskCD.Add(task.TaskCD, taskRecurringItems);
							}
							taskRecurringItems.Add(item);
						}
					}

					foreach (KeyValuePair<string, List<PMRecurringItem>> taskRecurringItems in srcRecurringItemsByTaskCD)
					{
						PMTask destTask;
						if (destTasks.TryGetValue(taskRecurringItems.Key, out destTask))
						{
							foreach (PMRecurringItem detail in taskRecurringItems.Value)
							{
								PMRecurringItem newDetail = new PMRecurringItem();
								newDetail.ProjectID = Project.Current.ContractID;
								newDetail.TaskID = destTask.TaskID;
								newDetail.InventoryID = detail.InventoryID;
								newDetail.UOM = detail.UOM;
								newDetail.Description = detail.Description;
								newDetail.Amount = detail.Amount;
								newDetail.AccountSource = detail.AccountSource;
								newDetail.SubMask = detail.SubMask;
								newDetail.AccountID = detail.AccountID;
								newDetail.SubID = detail.SubID;
								newDetail.BranchID = detail.BranchID;
								newDetail.ResetUsage = detail.ResetUsage;
								newDetail.Included = detail.Included;

								BillingItems.Insert(newDetail);
							}
						}
					}
				}

				OnDefaultFromTemplateTasksInserted(prj, templ, taskIds);
			}

			if (settings.CopyCurrency)
			{
				Project.Cache.SetValueExt<PMProject.curyID>(prj, templ.CuryID);
				Project.Cache.SetValueExt<PMProject.curyIDCopy>(prj, templ.CuryIDCopy);
				Project.Cache.SetDefaultExt<PMProject.billingCuryID>(prj);
			}

			if (settings.CopyEmployees)
			{
			foreach (EPEmployeeContract rate in PXSelect<EPEmployeeContract, Where<EPEmployeeContract.contractID, Equal<Current<PMProject.contractID>>>>.SelectMultiBound(this, new object[] { templ }))
			{
				EPEmployeeContract dst = EmployeeContract.Insert(new EPEmployeeContract());
				dst.EmployeeID = rate.EmployeeID;
			}
				EmployeeContract.Cache.Normalize();

			foreach (EPContractRate rate in PXSelect<EPContractRate, Where<EPContractRate.contractID, Equal<Current<PMProject.contractID>>>>.SelectMultiBound(this, new object[] { templ }))
			{
				EPContractRate dst = ContractRates.Insert(new EPContractRate());
				dst.IsActive = rate.IsActive;
				dst.EmployeeID = rate.EmployeeID;
				dst.LabourItemID = rate.LabourItemID;
				dst.EarningType = rate.EarningType;
				dst.ContractID = prj.ContractID;
			}
			}

			if (settings.CopyEquipment)
			{
			foreach (EPEquipmentRate equipment in PXSelect<EPEquipmentRate, Where<EPEquipmentRate.projectID, Equal<Current<PMProject.contractID>>>>.SelectMultiBound(this, new object[] { templ }))
			{
				EPEquipmentRate dst = EquipmentRates.Insert(new EPEquipmentRate());
				dst.IsActive = equipment.IsActive;
				dst.EquipmentID = equipment.EquipmentID;
				dst.RunRate = equipment.RunRate;
				dst.SuspendRate = equipment.SuspendRate;
				dst.SetupRate = equipment.SetupRate;
			}
			}

			if (settings.CopyAccountMapping)
			{
			foreach (PMAccountTask acc in PXSelect<PMAccountTask, Where<PMAccountTask.projectID, Equal<Current<PMProject.contractID>>>>.SelectMultiBound(this, new object[] { templ }))
			{
				if (taskIds.ContainsKey(acc.TaskID.GetValueOrDefault()) && !IsImport)
				{
					PMAccountTask dst = (PMAccountTask)Accounts.Cache.Insert();
					dst.AccountID = acc.AccountID;
					dst.TaskID = taskIds[acc.TaskID.GetValueOrDefault()];
				}
			}
			}

			if(settings.CopyNotification)
				DefaultFromTemplateNotificationSettings(templ);

			foreach (PMRetainageStep step in RetainageSteps.Select())
			{
				RetainageSteps.Delete(step);
			}

			foreach (PMRetainageStep step in RetainageSteps.View.SelectMultiBound(new object[] { templ }))
			{
				step.ProjectID = prj.ContractID;
				step.NoteID = null;
				RetainageSteps.Insert(step);
			}
		}

		public virtual void DefaultFromTemplateProjectSettings(PMProject prj, PMProject templ)
		{
			prj.Description = templ.Description;
			PXDBLocalizableStringAttribute.CopyTranslations<PMProject.description, PMProject.description>
				(Caches[typeof(PMProject)], templ, Caches[typeof(PMProject)], prj);
			prj.BudgetLevel = templ.BudgetLevel;
			prj.CostBudgetLevel = templ.CostBudgetLevel;
			prj.TermsID = templ.TermsID;
			prj.AutoAllocate = templ.AutoAllocate;
			prj.LimitsEnabled = templ.LimitsEnabled;
			prj.PrepaymentEnabled = templ.PrepaymentEnabled;
			prj.PrepaymentDefCode = templ.PrepaymentDefCode;
			prj.DefaultBranchID = templ.DefaultBranchID;
			prj.DefaultSalesAccountID = templ.DefaultSalesAccountID;
			prj.DefaultSalesSubID = templ.DefaultSalesSubID;
			prj.DefaultExpenseAccountID = templ.DefaultExpenseAccountID;
			prj.DefaultExpenseSubID = templ.DefaultExpenseSubID;
			prj.DefaultAccrualAccountID = templ.DefaultAccrualAccountID;
			prj.DefaultAccrualSubID = templ.DefaultAccrualSubID;
			prj.CalendarID = templ.CalendarID;
			prj.RestrictToEmployeeList = templ.RestrictToEmployeeList;
			prj.RestrictToResourceList = templ.RestrictToResourceList;
			prj.AllowOverrideCury = templ.AllowOverrideCury;
			prj.AllowOverrideRate = templ.AllowOverrideRate;
			prj.RateTableID = templ.RateTableID;
			prj.AllocationID = templ.AllocationID;
			prj.BillingID = templ.BillingID;
			prj.ApproverID = templ.ApproverID;
			prj.OwnerID = templ.OwnerID;
			prj.AutomaticReleaseAR = templ.AutomaticReleaseAR;
			prj.CreateProforma = templ.CreateProforma;
			prj.ChangeOrderWorkflow = templ.ChangeOrderWorkflow;
			prj.RetainagePct = templ.RetainagePct;
			prj.BudgetMetricsEnabled = templ.BudgetMetricsEnabled;
			prj.IncludeCO = templ.IncludeCO;
			prj.RetainageMaxPct = templ.RetainageMaxPct;
			prj.RetainageMode = templ.RetainageMode;
			prj.SteppedRetainage = templ.SteppedRetainage;
			prj.AIALevel = templ.AIALevel;
			prj.LastProformaNumber = templ.LastProformaNumber;
			prj.IncludeQtyInAIA = templ.IncludeQtyInAIA;

			prj.DropshipExpenseAccountSource = templ.DropshipExpenseAccountSource;
			prj.DropshipExpenseSubMask = templ.DropshipExpenseSubMask;
			prj.DropshipReceiptProcessing = templ.DropshipReceiptProcessing;
			prj.DropshipExpenseRecording = templ.DropshipExpenseRecording;

			prj.VisibleInAP = templ.VisibleInAP;
			prj.VisibleInGL = templ.VisibleInGL;
			prj.VisibleInAR = templ.VisibleInAR;
			prj.VisibleInSO = templ.VisibleInSO;
			prj.VisibleInPO = templ.VisibleInPO;
			prj.VisibleInTA = templ.VisibleInTA;
			prj.VisibleInEA = templ.VisibleInEA;
			prj.VisibleInIN = templ.VisibleInIN;
			prj.VisibleInCA = templ.VisibleInCA;
			prj.VisibleInCR = templ.VisibleInCR;

			ContractBillingSchedule billing = PXSelect<ContractBillingSchedule, Where<ContractBillingSchedule.contractID, Equal<Current<PMProject.contractID>>>>.SelectSingleBound(this, new object[] { templ });
			if (billing != null)
			{
				if (Billing.Current == null)
				{
					Billing.Current = Billing.Select();
				}
				if (Billing.Current != null)
				{
					Billing.SetValueExt<ContractBillingSchedule.type>(Billing.Current, billing.Type);
					Billing.Update(Billing.Current);
				}
			}
		}

		public virtual void DefaultFromTemplateNotificationSettings(PMProject templ)
		{
			//Delete Existing:
			foreach (NotificationSource source in NotificationSources.Select())
			{
				foreach (NotificationRecipient recipient in NotificationRecipients.Select(source.SourceID))
				{
					NotificationRecipients.Delete(recipient);
				}
				NotificationSources.Delete(source);
			}
			
			//Load from Template:
			var selectSource = new PXSelect<NotificationSource, Where<NotificationSource.refNoteID, Equal<Required<NotificationSource.refNoteID>>>>(this);
			var selectRecipients = new PXSelect<NotificationRecipient, Where<NotificationRecipient.sourceID, Equal<Required<NotificationRecipient.sourceID>>>>(this);
			foreach (NotificationSource source in selectSource.Select(templ.NoteID))
			{
				int? sourceID = source.SourceID;
				source.SourceID = null;
				source.RefNoteID = null;
				NotificationSource newsource = NotificationSources.Insert(source);

				//delete recepients added automatically from the PMSetup
				foreach (NotificationRecipient recipient in NotificationRecipients.Select(newsource.SourceID))
				{
					NotificationRecipients.Delete(recipient);
				}

				foreach (NotificationRecipient recipient in selectRecipients.Select(sourceID))
				{
					recipient.NotificationID = null;
					recipient.SourceID = newsource.SourceID;
					recipient.RefNoteID = null;

					NotificationRecipients.Insert(recipient);
				}
			}
		}

		public virtual bool Validate()
		{
			if (Project.Current != null && Billing.Current != null && Project.Current.CustomerID != null && string.IsNullOrEmpty(Billing.Current.Type))
			{
				Billing.Cache.RaiseExceptionHandling<ContractBillingSchedule.type>(Billing.Current, null, new PXSetPropertyException(ErrorMessages.FieldIsEmpty, $"[{nameof(ContractBillingSchedule.type)}]"));
				return false;
			}

			return true;
		}

		public override void Persist()
		{
			if (!Validate())
			{
				throw new PXException(Messages.ValidationFailed);
			}

			if (Project.Current != null && Project.Current.BudgetMetricsEnabled == true)
			{
				foreach (PMCostBudget budget in CostBudget.Cache.Inserted)
				{
					RecordProductionRecord(budget);
				}

				foreach (PMCostBudget budget in CostBudget.Cache.Updated)
				{
					if (UpdateLastXXXFields(budget))
					{
						RecordProductionRecord(budget);
					}
				}
			}

			costBudgetsByRevenueTaskID = new Dictionary<int, List<PMCostBudget>>();
			foreach (PMCostBudget budget in CostBudget.Cache.Inserted)
			{
				if (budget.RevenueTaskID < 0)
				{
					List<PMCostBudget> taskCostBudgets;
					if (!costBudgetsByRevenueTaskID.TryGetValue(budget.RevenueTaskID.Value, out taskCostBudgets))
					{
						taskCostBudgets = new List<PMCostBudget>();
						costBudgetsByRevenueTaskID.Add(budget.RevenueTaskID.Value, taskCostBudgets);
					}
					taskCostBudgets.Add(budget);
				}
			}

			foreach (PMCostBudget budget in CostBudget.Cache.Updated)
			{
				if (budget.RevenueTaskID < 0)
				{
					List<PMCostBudget> taskCostBudgets;
					if (!costBudgetsByRevenueTaskID.TryGetValue(budget.RevenueTaskID.Value, out taskCostBudgets))
					{
						taskCostBudgets = new List<PMCostBudget>();
						costBudgetsByRevenueTaskID.Add(budget.RevenueTaskID.Value, taskCostBudgets);
					}
					taskCostBudgets.Add(budget);
				}
			}

			foreach(PMBudgetProduction line in BudgetProduction.Cache.Deleted)
			{
				BudgetProduction.Cache.PersistDeleted(line);
			}

			base.Persist();
		}

		public bool _IsRecalculatingRevenueBudgetScope = false;
		public virtual void RecalculateRevenueBudget(PMRevenueBudget row)
		{
			if (row != null)
			{
				int? projectTaskID = row.ProjectTaskID;
				if (projectTaskID == null)
				{
					//Current record may be in process of importing from excel. In this case all we have is pending values for TaskCD
					object pendingValue = RevenueBudget.Cache.GetValuePending<PMRevenueBudget.projectTaskID>(row);
					PMTask task = PXSelect<PMTask, Where<PMTask.projectID, Equal<Current<PMProject.contractID>>, And<PMTask.taskCD, Equal<Required<PMTask.taskCD>>>>>.Select(this, pendingValue);
					if (task != null)
					{
						projectTaskID = task.TaskID;
					}
				}

				try
				{
					_IsRecalculatingRevenueBudgetScope = true;
					if (row.ProgressBillingBase == ProgressBillingBase.Amount && ContainsProgressiveBillingRule(row.ProjectID, projectTaskID))
					{
						decimal budgetedAmount = row.CuryRevisedAmount.GetValueOrDefault();
						decimal invoicedOrPendingPrepayment = row.CuryActualAmount.GetValueOrDefault() + row.CuryInvoicedAmount.GetValueOrDefault() + (row.CuryPrepaymentAmount.GetValueOrDefault() - row.CuryPrepaymentInvoiced.GetValueOrDefault());
						decimal amountToInvoice = (budgetedAmount * row.CompletedPct.GetValueOrDefault() / 100m) - invoicedOrPendingPrepayment;
						RevenueBudget.SetValueExt<PMRevenueBudget.curyAmountToInvoice>(row, amountToInvoice);
					}
					else if (row.ProgressBillingBase == ProgressBillingBase.Quantity)
					{
						decimal newQty;
						if (_BlockQtyToInvoiceCalculate == true)
						{
							newQty = row.QtyToInvoice.GetValueOrDefault();
						}
						else
						{
							newQty = (row.CompletedPct.GetValueOrDefault() / 100.0m * row.RevisedQty.GetValueOrDefault()) - (row.InvoicedQty.GetValueOrDefault() + row.ActualQty.GetValueOrDefault());
						newQty = decimal.Round(newQty, CommonSetupDecPl.Qty);

						RevenueBudget.SetValueExt<PMRevenueBudget.qtyToInvoice>(row, newQty);
						}

						RevenueBudget.SetValueExt<PMRevenueBudget.curyAmountToInvoice>(row, newQty * row.CuryUnitRate.GetValueOrDefault());
					}
				}
				finally
				{
					_IsRecalculatingRevenueBudgetScope = false;
				}

			}
		}

		public virtual bool ContainsProgressiveBillingRule(int? projectID, int? taskID)
		{
			PMTask task = PMTask.PK.FindDirty(this, projectID, taskID);

			if (task != null && !string.IsNullOrEmpty(task.BillingID))
			{
				PMBillingRule topProgressive = PXSelect<PMBillingRule, Where<PMBillingRule.billingID, Equal<Required<PMBillingRule.billingID>>, And<PMBillingRule.type, Equal<PMBillingType.budget>>>>.SelectWindowed(this, 0, 1, task.BillingID);

				return topProgressive != null;
			}

			return false;
		}

		private PMProject GetProjectByID(int? id)
		{
			if (id == null)
				return null;

			return PMProject.PK.Find(this, id);
		}

		private bool CanBeBilled()
		{
			if (Project.Current != null)
			{
				if (Project.Current.IsActive == false)
				{
					throw new PXException(Messages.InactiveProjectsCannotBeBilled);
				}

				if (Project.Current.IsCancelled == true)
				{
					throw new PXException(Messages.CancelledProjectsCannotBeBilled);
				}

				if (Project.Cache.GetStatus(Project.Current) == PXEntryStatus.Inserted)
					return false;

				if (Project.Current.CustomerID == null)
				{
					throw new PXException(Messages.NoCustomer);
				}
				else
				{
					ContractBillingSchedule billingCurrent = Billing.Select();

					if (billingCurrent != null)
					{
						if (billingCurrent.NextDate == null && billingCurrent.Type != BillingType.OnDemand)
							throw new PXException(Messages.NoNextBillDateProjectCannotBeBilled);
					}
					else
					{
						return false;
					}

					return true;
				}
			}

			return false;
		}

		public virtual bool UpdateLastXXXFields(PMCostBudget budget)
		{
			decimal? originalCuryCostToComplete = (decimal?)CostBudget.Cache.GetValueOriginal<PMCostBudget.curyCostToComplete>(budget);
			decimal? originalCostToComplete = (decimal?)CostBudget.Cache.GetValueOriginal<PMCostBudget.costToComplete>(budget);
			decimal? originalPercentCompleted = (decimal?)CostBudget.Cache.GetValueOriginal<PMCostBudget.percentCompleted>(budget);
			decimal? originalCuryCostAtCompletion = (decimal?)CostBudget.Cache.GetValueOriginal<PMCostBudget.curyCostAtCompletion>(budget);
			decimal? originalCostAtCompletion = (decimal?)CostBudget.Cache.GetValueOriginal<PMCostBudget.costAtCompletion>(budget);

			bool updated = false;
			if (budget.CuryCostToComplete != originalCuryCostToComplete ||
				budget.PercentCompleted != originalPercentCompleted ||
				budget.CuryCostAtCompletion != originalCuryCostAtCompletion)
			{
				budget.CuryLastCostToComplete = originalCuryCostToComplete;
				budget.LastCostToComplete = originalCostToComplete;
				budget.LastPercentCompleted = originalPercentCompleted;
				budget.CuryLastCostAtCompletion = originalCuryCostAtCompletion;
				budget.LastCostAtCompletion = originalCostAtCompletion;
				updated = true;
			}
			return updated;
		}

		public virtual PMBudgetProduction RecordProductionRecord(PMCostBudget budget)
		{
			CostBudget.Cache.SetValue<PMCostBudget.lineCntr>(budget, budget.LineCntr.GetValueOrDefault() + 1);
			CostBudget.Cache.MarkUpdated(budget);

			PMBudgetProduction item = new PMBudgetProduction();
			item.ProjectID = budget.ProjectID;
			item.ProjectTaskID = budget.ProjectTaskID;
			item.AccountGroupID = budget.AccountGroupID;
			item.CostCodeID = budget.CostCodeID;
			item.InventoryID = budget.InventoryID;
			item.CuryCostToComplete = budget.CuryCostToComplete;
			item.CostToComplete = budget.CostToComplete;
			item.PercentCompleted = budget.PercentCompleted;
			item.CuryCostAtCompletion = budget.CuryCostAtCompletion;
			item.CostAtCompletion = budget.CostAtCompletion;
			item.LineNbr = budget.LineCntr;

			return BudgetProduction.Insert(item);
		}
		        
		public virtual void AddToSummary(PMBudget summary, PMBudget record)
		{
			if (summary == null) throw new ArgumentNullException(nameof(summary));
			if (record == null) throw new ArgumentNullException(nameof(record));

			summary.CuryAmount = summary.CuryAmount.GetValueOrDefault() + record.CuryAmount.GetValueOrDefault();
			summary.CuryRevisedAmount = summary.CuryRevisedAmount.GetValueOrDefault() + record.CuryRevisedAmount.GetValueOrDefault();
			summary.CuryActualAmount = summary.CuryActualAmount.GetValueOrDefault() + record.CuryActualAmount.GetValueOrDefault();
			summary.ActualAmount = summary.ActualAmount.GetValueOrDefault() + record.ActualAmount.GetValueOrDefault();
			summary.CuryCommittedAmount = summary.CuryCommittedAmount.GetValueOrDefault() + record.CuryCommittedAmount.GetValueOrDefault();
			summary.CommittedAmount = summary.CommittedAmount.GetValueOrDefault() + record.CommittedAmount.GetValueOrDefault();
			summary.CuryCommittedOrigAmount = summary.CuryCommittedOrigAmount.GetValueOrDefault() + record.CuryCommittedOrigAmount.GetValueOrDefault();
			summary.CuryCommittedOpenAmount = summary.CuryCommittedOpenAmount.GetValueOrDefault() + record.CuryCommittedOpenAmount.GetValueOrDefault();
			summary.CuryCommittedInvoicedAmount = summary.CuryCommittedInvoicedAmount.GetValueOrDefault() + record.CuryCommittedInvoicedAmount.GetValueOrDefault();
			summary.CuryChangeOrderAmount = summary.CuryChangeOrderAmount.GetValueOrDefault() + record.CuryChangeOrderAmount.GetValueOrDefault();
			summary.CuryDraftChangeOrderAmount = summary.CuryDraftChangeOrderAmount.GetValueOrDefault() + record.CuryDraftChangeOrderAmount.GetValueOrDefault();
			summary.CuryInvoicedAmount = summary.CuryInvoicedAmount.GetValueOrDefault() + record.CuryInvoicedAmount.GetValueOrDefault();
			summary.InvoicedAmount = summary.InvoicedAmount.GetValueOrDefault() + record.InvoicedAmount.GetValueOrDefault();
			summary.CuryAmountToInvoice = summary.CuryAmountToInvoice.GetValueOrDefault() + record.CuryAmountToInvoice.GetValueOrDefault();
			summary.AmountToInvoice = summary.AmountToInvoice.GetValueOrDefault() + record.AmountToInvoice.GetValueOrDefault();
			summary.CuryMaxAmount = summary.CuryMaxAmount.GetValueOrDefault() + record.CuryMaxAmount.GetValueOrDefault();
			summary.MaxAmount = summary.MaxAmount.GetValueOrDefault() + record.MaxAmount.GetValueOrDefault();
			if (string.IsNullOrEmpty(summary.Description))
			{
				summary.Description = record.Description;
			}
		}

		public virtual List<PMBudget> AggregateBudget<T>(IList<T> list)
			where T: PMBudget, new()
		{
			Dictionary<int, PMTask> tasks = new Dictionary<int, PMTask>();
			foreach(PMTask task in Tasks.Select())
			{
				tasks.Add(task.TaskID.Value, task);
			}

			Dictionary<int, PMBudget> aggregates = new Dictionary<int, PMBudget>();

			T total = new T();
			total.ProjectID = Project.Current.ContractID;
			total.ProjectTaskID = null;
			total.AccountGroupID = null;
			total.CostCodeID = null;
			total.InventoryID = null;
			total.Description = Messages.Total;
			total.SortOrder = 1;
						
			foreach (PMBudget budget in list)
			{
				int key = budget.ProjectTaskID.Value;

				PMBudget summary = null;
				if (!aggregates.TryGetValue(key, out summary))
				{
					summary = new T();
					summary.ProjectID = budget.ProjectID;
					summary.ProjectTaskID = budget.ProjectTaskID;
					summary.AccountGroupID = budget.AccountGroupID;
					summary.CostCodeID = null;
					summary.InventoryID = null;

					PMTask task;
					if (budget.ProjectTaskID != null && tasks.TryGetValue(budget.ProjectTaskID.Value, out task))
					{
						summary.Description = task.Description;
					}
					
					aggregates.Add(key, summary);
				}

				AddToSummary(summary, budget);
				AddToSummary(total, budget);

				if (Setup.Current.AutoCompleteRevenueBudget == true)
				{
					summary.CuryAmountToInvoice = 0;
				}
			}

			if (Setup.Current.AutoCompleteRevenueBudget == true)
			{
				total.CuryAmountToInvoice = 0;

				List<PMRevenueTotal> amountToInvoiceProjections = GetAutoRevenueTotals();
				foreach(PMRevenueTotal item in amountToInvoiceProjections)
				{
					aggregates[item.ProjectTaskID.Value].CuryAmountToInvoice += item.CuryAmountToInvoiceProjected.GetValueOrDefault();
					total.CuryAmountToInvoice += item.CuryAmountToInvoiceProjected.GetValueOrDefault();
				}
			}

			List<PMBudget> result = new List<PMBudget>();
			result.AddRange(aggregates.Values);
			result.Add(total);

			return result;
		}

		protected virtual List<PMRevenueTotal> GetAutoRevenueTotals()
		{
			List<PMRevenueTotal> list = new List<PMRevenueTotal>();
			var ratios = CalculateCostCompletedPercent();

			var select = new PXSelect<PMRevenueTotal, Where<PMRevenueTotal.projectID, Equal<Current<PMProject.contractID>>>>(this);

			foreach (PMRevenueTotal revenue in select.Select())
			{
				string key = string.Format("{0}.{1}", revenue.ProjectTaskID, revenue.InventoryID.GetValueOrDefault(PMInventorySelectorAttribute.EmptyInventoryID));
				decimal completedPct;
				if (ratios.TryGetValue(key, out completedPct))
				{
					decimal budgetedAmount = revenue.CuryRevisedAmount.GetValueOrDefault();
					revenue.CuryAmountToInvoiceProjected = Math.Max(0, (budgetedAmount * completedPct * 0.01m) - revenue.CuryInvoicedAmount.GetValueOrDefault());
				}

				list.Add(revenue);
			}

			return list;
		}

		protected virtual decimal CalculateCompletedPercentByCost(PMRevenueBudget row)
		{
			var select = new PXSelect<PMProductionBudget,
					Where<PMProductionBudget.projectID, Equal<Current<PMProject.contractID>>,
					And<PMProductionBudget.revenueTaskID, Equal<Required<PMProductionBudget.revenueTaskID>>,
					And<PMProductionBudget.revenueInventoryID, Equal<Required<PMProductionBudget.revenueInventoryID>>>>>>(this);

			PMProductionBudget ratio = select.Select(row.ProjectTaskID, row.InventoryID);

			decimal result = 0;
			if (ratio != null)
			{
				decimal revisedTotal = ratio.CuryRevisedAmount.GetValueOrDefault();
				decimal actualTotal = ratio.CuryActualAmount.GetValueOrDefault();

				if (revisedTotal != 0)
				{
					result = Decimal.Round(100m * actualTotal / revisedTotal, 2);
				}
			}

			return result;
		}

		protected virtual Dictionary<string, decimal> CalculateCostCompletedPercent()
		{
			var selectCostBudgetWithProduction = new PXSelect<PMProductionBudget, 
				Where<PMProductionBudget.projectID, Equal<Current<PMProject.contractID>>>>(this);

			var ratios = new Dictionary<string, decimal>();

			foreach (PMProductionBudget total in selectCostBudgetWithProduction.Select())
			{
				string key = string.Format("{0}.{1}", total.RevenueTaskID, total.RevenueInventoryID.GetValueOrDefault(PMInventorySelectorAttribute.EmptyInventoryID));
				decimal ratio = 0;

				if (total.CuryRevisedAmount.GetValueOrDefault() != 0)
				{
					ratio = decimal.Round(100m * total.CuryActualAmount.GetValueOrDefault() / total.CuryRevisedAmount.GetValueOrDefault(), 2);
				}

				ratios.Add(key, ratio);
			}

			return ratios;
		}

		protected virtual decimal? CalculateCapAmount(PMProject project, PMProjectRevenueTotal totals)
		{
			decimal? result = null;
			if (project != null)
			{
				decimal contractAmt = 0;

				if (totals != null)
				{
					contractAmt = project.IncludeCO == true ? totals.CuryRevisedAmount.GetValueOrDefault() : totals.CuryAmount.GetValueOrDefault();
				}

				if (contractAmt > 0)
				{
					result = Decimal.Round(contractAmt * project.RetainageMaxPct.GetValueOrDefault() * project.RetainagePct.GetValueOrDefault() * 0.01m * 0.01m, 2);
				}
			}

			return result;
		}

		protected virtual void OnProjectCDChanged()
		{
			foreach (PXResult<INCostCenter, PMTask, IN.INLocation> res in CostCenters.Select())
			{
				var costCenter = (INCostCenter)res;
				PMTask task = (PMTask)res;
				IN.INLocation location = (IN.INLocation)res;

				costCenter.CostCenterCD = BuildCostCenterCD(Project.Current, task, location);
				CostCenters.Update(costCenter);
			}
		}

		protected virtual string BuildCostCenterCD(PMProject project, PMTask task, IN.INLocation location)
		{
			INSite site = INSite.PK.Find(this, location.SiteID);
			return string.Format("{0}/{1}/{2}/{3}", project.ContractCD.Trim(), task.TaskCD.Trim(), site.SiteCD.Trim(), location.LocationCD.Trim());
		}

		#region Local Types
		[Serializable]
		[PXCacheName(Messages.PMProjectBalance)]
		[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		public class PMProjectBalanceRecord : IBqlTable
		{
			public const int EmptyInventoryID = 0;

			#region RecordID
			public abstract class recordID : PX.Data.BQL.BqlInt.Field<recordID> { }
			protected Int32? _RecordID;
			[PXInt(IsKey = true)]
			public virtual Int32? RecordID
			{
				get
				{
					return this._RecordID;
				}
				set
				{
					this._RecordID = value;
				}
			}
			#endregion
			#region AccountGroup
			public abstract class accountGroup : PX.Data.BQL.BqlString.Field<accountGroup> { }
			protected string _AccountGroup;
			[PXString]
			[PXUIField(DisplayName = "Account Group")]
			public virtual string AccountGroup
			{
				get
				{
					return this._AccountGroup;
				}
				set
				{
					this._AccountGroup = value;
				}
			}
			#endregion
			#region SortOrder
			public abstract class sortOrder : PX.Data.BQL.BqlInt.Field<sortOrder> { }
			protected Int32? _SortOrder;
			[PXInt()]
			public virtual Int32? SortOrder
			{
				get
				{
					return this._SortOrder;
				}
				set
				{
					this._SortOrder = value;
				}
			}
			#endregion

			#region Description
			public abstract class description : PX.Data.BQL.BqlString.Field<description> { }
			protected String _Description;
			[PXString(255, IsUnicode = true)]
			[PXUIField(DisplayName = "Description")]
			public virtual String Description
			{
				get
				{
					return this._Description;
				}
				set
				{
					this._Description = value;
				}
			}
			#endregion
			#region CuryAmount
			public abstract class curyAmount : PX.Data.BQL.BqlDecimal.Field<curyAmount>
			{
			}
			[PXCurrency(typeof(PMProject.curyInfoID), typeof(amount))]
			[PXUIField(DisplayName = "Original Budgeted Amount")]
			public virtual Decimal? CuryAmount
			{
				get;
				set;
			}
			#endregion
			#region Amount
			public abstract class amount : PX.Data.BQL.BqlDecimal.Field<amount> { }
			[PXBaseCury]
			[PXUIField(DisplayName = "Original Budgeted Amount in Base Currency")]
			public virtual Decimal? Amount
			{
				get;
				set;
			}
			#endregion

			#region CuryDraftCOAmount
			public abstract class curyDraftCOAmount : PX.Data.BQL.BqlDecimal.Field<curyDraftCOAmount>
			{
			}

			[PXCurrency(typeof(PMProject.curyInfoID), typeof(draftCOAmount))]
			[PXUIField(DisplayName = "Potential CO Amount")]
			public virtual Decimal? CuryDraftCOAmount
			{
				get;
				set;
			}
			#endregion
			#region DraftCOAmount
			public abstract class draftCOAmount : PX.Data.BQL.BqlDecimal.Field<draftCOAmount> { }
			[PXBaseCury]
			[PXUIField(DisplayName = "Potential CO Amount in Base Currency")]
			public virtual Decimal? DraftCOAmount
			{
				get;
				set;
			}
			#endregion

			#region CuryBudgetedCOAmount
			public abstract class curyBudgetedCOAmount : PX.Data.BQL.BqlDecimal.Field<curyBudgetedCOAmount>
			{
			}

			[PXCurrency(typeof(PMProject.curyInfoID), typeof(budgetedCOAmount))]
			[PXUIField(DisplayName = "Budgeted CO Amount")]
			public virtual Decimal? CuryBudgetedCOAmount
			{
				get;
				set;
			}
			#endregion
			#region BudgetedCOAmount
			public abstract class budgetedCOAmount : PX.Data.BQL.BqlDecimal.Field<budgetedCOAmount> { }
			[PXBaseCury]
			[PXUIField(DisplayName = "Budgeted CO Amount in Base Currency")]
			public virtual Decimal? BudgetedCOAmount
			{
				get;
				set;
			}
			#endregion

			#region CuryRevisedAmount
			public abstract class curyRevisedAmount : PX.Data.BQL.BqlDecimal.Field<curyRevisedAmount>
			{
			}
			[PXCurrency(typeof(PMProject.curyInfoID), typeof(revisedAmount))]
			[PXUIField(DisplayName = "Revised Budgeted Amount")]
			public virtual Decimal? CuryRevisedAmount
			{
				get;
				set;
			}
			#endregion
			#region RevisedAmount
			public abstract class revisedAmount : PX.Data.BQL.BqlDecimal.Field<revisedAmount> { }
			[PXBaseCury]
			[PXUIField(DisplayName = "Revised Budgeted Amount")]
			public virtual Decimal? RevisedAmount
			{
				get;
				set;
			}
			#endregion

			#region CuryActualAmount
			public abstract class curyActualAmount : PX.Data.BQL.BqlDecimal.Field<curyActualAmount>
			{
			}
			[PXCurrency(typeof(PMProject.curyInfoID), typeof(baseActualAmount))]
			[PXUIField(DisplayName = "Actual Amount", Enabled = false)]
			public virtual Decimal? CuryActualAmount
			{
				get;
				set;
			}
			#endregion
			#region ActualAmount
			public abstract class actualAmount : PX.Data.BQL.BqlDecimal.Field<actualAmount> { }
			[PXBaseCury]
			[PXUIField(DisplayName = "Hist. Actual Amount in Base Currency", Enabled = false, FieldClass = nameof(FeaturesSet.ProjectMultiCurrency))]
			public virtual Decimal? ActualAmount
			{
				get;
				set;
			}
			#endregion
			#region BaseActualAmount
			public abstract class baseActualAmount : PX.Data.BQL.BqlDecimal.Field<baseActualAmount> { }
			[PXBaseCury]
			[PXUIField(DisplayName = "Actual Amount", Enabled = false)]
			public virtual Decimal? BaseActualAmount
			{
				get;
				set;
			}
			#endregion

			#region CuryCommittedAmount
			public abstract class curyCommittedAmount : PX.Data.BQL.BqlDecimal.Field<curyCommittedAmount>
			{
			}
			[PXCurrency(typeof(PMProject.curyInfoID), typeof(committedAmount))]
			[PXUIField(DisplayName = "Revised Committed Amount", Enabled = false)]
			public virtual Decimal? CuryCommittedAmount
			{
				get;
				set;
			}
			#endregion
			#region CommittedAmount
			public abstract class committedAmount : PX.Data.BQL.BqlDecimal.Field<committedAmount> { }
			[PXBaseCury]
			[PXUIField(DisplayName = "Revised Committed Amount in Base Currency", Enabled = false)]
			public virtual Decimal? CommittedAmount
			{
				get;
				set;
			}
			#endregion

			#region CuryCommittedOpenAmount
			public abstract class curyCommittedOpenAmount : PX.Data.BQL.BqlDecimal.Field<curyCommittedOpenAmount>
			{
			}
			[PXCurrency(typeof(PMProject.curyInfoID), typeof(committedOpenAmount))]
			[PXUIField(DisplayName = "Committed Open Amount", Enabled = false)]
			public virtual Decimal? CuryCommittedOpenAmount
			{
				get;
				set;
			}
			#endregion
			#region CommittedOpenAmount
			public abstract class committedOpenAmount : PX.Data.BQL.BqlDecimal.Field<committedOpenAmount> { }
			[PXBaseCury]
			[PXUIField(DisplayName = "Committed Open Amount in Base Currency", Enabled = false)]
			public virtual Decimal? CommittedOpenAmount
			{
				get;
				set;
			}
			#endregion
			#region CuryCommittedInvoicedAmount
			public abstract class curyCommittedInvoicedAmount : PX.Data.BQL.BqlDecimal.Field<curyCommittedInvoicedAmount>
			{
			}
			[PXCurrency(typeof(PMProject.curyInfoID), typeof(committedInvoicedAmount))]
			[PXUIField(DisplayName = "Committed Invoiced Amount", Enabled = false)]
			public virtual Decimal? CuryCommittedInvoicedAmount
			{
				get;
				set;
			}
			#endregion
			#region CommittedInvoicedAmount
			public abstract class committedInvoicedAmount : PX.Data.BQL.BqlDecimal.Field<committedInvoicedAmount> { }
			[PXBaseCury]
			[PXUIField(DisplayName = "Committed Invoiced Amount in Base Currency", Enabled = false)]
			public virtual Decimal? CommittedInvoicedAmount
			{
				get;
				set;
			}
			#endregion

			#region CuryOriginalCommittedAmount
			public abstract class curyOriginalCommittedAmount : PX.Data.BQL.BqlDecimal.Field<curyOriginalCommittedAmount>
			{
			}
			[PXCurrency(typeof(PMProject.curyInfoID), typeof(originalCommittedAmount))]
			[PXUIField(DisplayName = "Original Committed Amount")]
			public virtual Decimal? CuryOriginalCommittedAmount
			{
				get;
				set;
			}
			#endregion
			#region OriginalCommittedAmount
			public abstract class originalCommittedAmount : PX.Data.BQL.BqlDecimal.Field<originalCommittedAmount> { }
			[PXBaseCury]
			[PXUIField(DisplayName = "Original Committed Amount in Base Currency")]
			public virtual Decimal? OriginalCommittedAmount
			{
				get;
				set;
			}
			#endregion

			#region CuryCommittedCOAmount
			public abstract class curyCommittedCOAmount : PX.Data.BQL.BqlDecimal.Field<curyCommittedCOAmount>
			{
			}
			[PXCurrency(typeof(PMProject.curyInfoID), typeof(committedCOAmount))]
			[PXUIField(DisplayName = "Committed CO Amount")]
			public virtual Decimal? CuryCommittedCOAmount
			{
				get;
				set;
			}
			#endregion
			#region CommittedCOAmount
			public abstract class committedCOAmount : PX.Data.BQL.BqlDecimal.Field<committedCOAmount> { }
			[PXBaseCury]
			[PXUIField(DisplayName = "Committed CO Amount in Base Currency")]
			public virtual Decimal? CommittedCOAmount
			{
				get;
				set;
			}
			#endregion

			#region CuryActualPlusOpenCommittedAmount
			public abstract class curyActualPlusOpenCommittedAmount : PX.Data.BQL.BqlDecimal.Field<curyActualPlusOpenCommittedAmount>
			{
			}

			[PXCurrency(typeof(PMProject.curyInfoID), typeof(actualPlusOpenCommittedAmount))]
			[PXUIField(DisplayName = "Actual + Open Committed Amount", Enabled = false)]
			public virtual Decimal? CuryActualPlusOpenCommittedAmount
			{
				get
				{
					return this.CuryActualAmount + this.CuryCommittedOpenAmount;
				}
			}
			#endregion
			#region ActualPlusOpenCommittedAmount
			public abstract class actualPlusOpenCommittedAmount : PX.Data.BQL.BqlDecimal.Field<actualPlusOpenCommittedAmount> { }

			[PXBaseCury]
			[PXUIField(DisplayName = "Actual + Open Committed Amount in Base Currency", Enabled = false)]
			public virtual Decimal? ActualPlusOpenCommittedAmount
			{
				get
				{
					return this.ActualAmount + this.CommittedOpenAmount;
				}
			}
			#endregion
			#region CuryVarianceAmount
			public abstract class curyVarianceAmount : PX.Data.BQL.BqlDecimal.Field<curyVarianceAmount>
			{
			}

			[PXCurrency(typeof(PMProject.curyInfoID), typeof(varianceAmount))]
			[PXUIField(DisplayName = "Variance Amount", Enabled = false)]
			public virtual Decimal? CuryVarianceAmount
			{
				get
				{
					return this.CuryRevisedAmount - this.CuryActualPlusOpenCommittedAmount;
				}
			}
			#endregion
			#region VarianceAmount
			public abstract class varianceAmount : PX.Data.BQL.BqlDecimal.Field<varianceAmount> { }

			[PXBaseCury]
			[PXUIField(DisplayName = "Variance Amount", Enabled = false)]
			public virtual Decimal? VarianceAmount
			{
				get
				{
					return this.RevisedAmount - this.ActualPlusOpenCommittedAmount;
				}
			}
			#endregion


			#region Performance
			public abstract class performance : PX.Data.BQL.BqlDecimal.Field<performance> { }
			protected Decimal? _Performance;
			[PXDecimal(2)]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXUIField(DisplayName = "Performance (%)", Enabled = false)]
			public virtual Decimal? Performance
			{
				get
				{
					if (CuryRevisedAmount != 0)
						return (CuryActualAmount / CuryRevisedAmount) * 100;
					else
						return 0;
				}
			}
			#endregion
		}

		[PXHidden]
		[Serializable]
		[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		public class TemplateSettingsFilter : IBqlTable
		{
			#region TemplateID
			public abstract class templateID : PX.Data.BQL.BqlString.Field<templateID> { }
			protected string _TemplateID;
			[PXString()]
			[PXUIField(DisplayName = "Template ID", Required = true)]
			[PXDimensionAttribute(ProjectAttribute.DimensionNameTemplate)]
			public virtual string TemplateID
			{
				get
				{
					return this._TemplateID;
				}
				set
				{
					this._TemplateID = value;
				}
			}
			#endregion
		}

		[PXCacheName(Messages.CostBudgetFilter)]
		[Serializable]
		[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		public class CostBudgetFilter : IBqlTable
		{
			#region ProjectTaskID
			public abstract class projectTaskID : PX.Data.BQL.BqlInt.Field<projectTaskID> { }
			protected Int32? _ProjectTaskID;
			[ProjectTask(typeof(PMProject.contractID), AlwaysEnabled = true, DirtyRead = true)]
			public virtual Int32? ProjectTaskID
			{
				get
				{
					return this._ProjectTaskID;
				}
				set
				{
					this._ProjectTaskID = value;
				}
			}
			#endregion

			#region GroupByTask
			public abstract class groupByTask : PX.Data.BQL.BqlBool.Field<groupByTask> { }
			[PXDBBool()]
			[PXDefault(false)]
			[PXUIField(DisplayName = "Group by Task")]
			public virtual Boolean? GroupByTask
			{
				get;
				set;
			}
			#endregion
		}

		[PXCacheName(Messages.RevenueBudgetFilter)]
		[Serializable]
		[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		public class RevenueBudgetFilter : IBqlTable
		{
			#region ProjectTaskID
			public abstract class projectTaskID : PX.Data.BQL.BqlInt.Field<projectTaskID> { }
			protected Int32? _ProjectTaskID;
			[ProjectTask(typeof(PMProject.contractID), AlwaysEnabled = true, DirtyRead = true)]
			public virtual Int32? ProjectTaskID
			{
				get
				{
					return this._ProjectTaskID;
				}
				set
				{
					this._ProjectTaskID = value;
				}
			}
			#endregion

			#region GroupByTask
			public abstract class groupByTask : PX.Data.BQL.BqlBool.Field<groupByTask> { }
			[PXDBBool()]
			[PXDefault(false)]
			[PXUIField(DisplayName = "Group by Task")]
			public virtual Boolean? GroupByTask
			{
				get;
				set;
			}
			#endregion

			#region CuryAmountToInvoiceTotal
			public abstract class curyAmountToInvoiceTotal : PX.Data.BQL.BqlDecimal.Field<curyAmountToInvoiceTotal> { }
			[PXDBCurrency(typeof(PMProject.curyInfoID), typeof(RevenueBudgetFilter.amountToInvoiceTotal))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Pending Invoice Amount Total")]
			public virtual decimal? CuryAmountToInvoiceTotal
			{
				get;
				set;
			}
			#endregion
			#region AmountToInvoiceTotal
			public abstract class amountToInvoiceTotal : PX.Data.BQL.BqlDecimal.Field<amountToInvoiceTotal> { }
			[PXDBBaseCury]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Pending Invoice Amount Total in Base Currency")]
			public virtual decimal? AmountToInvoiceTotal
			{
				get;
				set;
			}
			#endregion
		}

        public class DefaultFromTemplateSettings
		{
			public bool CopyProperties { get; set; }
			public bool CopyTasks { get; set; }
			public bool CopyBudget { get; set; }
			public bool CopyAttributes { get; set; }
			public bool CopyEmployees { get; set; }
			public bool CopyEquipment { get; set; }
			public bool CopyNotification { get; set; }
			public bool CopyAccountMapping { get; set; }
			public bool CopyRecurring { get; set; }

			public bool CopyCurrency { get; set; }

			public static DefaultFromTemplateSettings Default
			{
				get { return new DefaultFromTemplateSettings() { CopyProperties = true, CopyTasks = true, CopyBudget = true,
					CopyAttributes =true, CopyRecurring=true, CopyEmployees=true, CopyEquipment=true, CopyNotification=true, CopyAccountMapping=true, CopyCurrency = true }; }
			}


		}

		[PXHidden]
		[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		public class LoadFromTemplateInfo : IBqlTable
		{
			#region TemplateID
			public abstract class templateID : PX.Data.BQL.BqlString.Field<templateID>
			{
			}

			[PXInt()]
			[PXUIField(DisplayName = "Template ID", Required = true)]
			[PXDimensionAttribute(ProjectAttribute.DimensionName)]
			public virtual int? TemplateID
			{
				get;
				set;
			}
			#endregion
		}

		[PXHidden]
		[Serializable]
		[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		public class CopyDialogInfo : IBqlTable
		{
			#region ProjectID
			public abstract class projectID : PX.Data.BQL.BqlString.Field<projectID>
			{
			}

			[PXString()]
			[PXUIField(DisplayName = "Project ID", Required = true)]
			[PXDimensionAttribute(ProjectAttribute.DimensionName)]
			public virtual string ProjectID
			{
				get;
				set;
			}
			#endregion
		}

		#endregion

		#region Project Balance

		public virtual IEnumerable GetBalanceLines(string accountType, List<PMProjectBalanceRecord> records)
		{
			if (records.Count > 0)
			{
				if (!IsMobile)
				{
					yield return CreateHeader(accountType);
				}

				decimal curyTotalAmt = 0;
				decimal curyTotalRevAmt = 0;
				decimal curyTotalActAmt = 0;
				decimal totalActAmt = 0;
				decimal curyTotalComAmt = 0;
				decimal curyTotalComOpenAmt = 0;
				decimal curyTotalComInvoicedAmt = 0;
				decimal curyTotalComOrigAmt = 0;
				decimal curyDraftCOAmt = 0;
				decimal curyBudgetedCOAmt = 0;
				decimal curyCommittedCOAmt = 0;

				foreach (PMProjectBalanceRecord record in records)
				{
					if (!IsMobile)
					{
						curyTotalAmt += record.CuryAmount ?? 0;
						curyTotalRevAmt += record.CuryRevisedAmount ?? 0;
						curyTotalActAmt += record.CuryActualAmount ?? 0;
						totalActAmt += record.ActualAmount ?? 0;
						curyTotalComAmt += record.CuryCommittedAmount ?? 0;
						curyTotalComOpenAmt += record.CuryCommittedOpenAmount ?? 0;
						curyTotalComInvoicedAmt += record.CuryCommittedInvoicedAmount ?? 0;
						curyTotalComOrigAmt += record.CuryOriginalCommittedAmount ?? 0;
						curyDraftCOAmt += record.CuryDraftCOAmount ?? 0;
						curyBudgetedCOAmt += record.CuryBudgetedCOAmount ?? 0;
						curyCommittedCOAmt += record.CuryCommittedCOAmount ?? 0;
					}

					yield return record;
				}

				if (!IsMobile)
				{
					yield return CreateTotal(accountType, curyTotalAmt, curyTotalRevAmt, curyTotalActAmt, totalActAmt, curyTotalComAmt, curyTotalComOpenAmt, curyTotalComInvoicedAmt, curyTotalComOrigAmt, curyDraftCOAmt, curyBudgetedCOAmt, curyCommittedCOAmt);
				}
			}
		}

		public virtual PMProjectBalanceRecord BalanceRecordFromBudget(PMBudget ps, PMAccountGroup ag)
		{
			PMProjectBalanceRecord record = new PMProjectBalanceRecord();
			record.RecordID = ps.AccountGroupID;
			record.AccountGroup = ag.GroupCD;
			record.Description = ag.Description;
			record.CuryAmount = ps.CuryAmount;
			record.Amount = ps.Amount;
			record.CuryRevisedAmount = ps.CuryRevisedAmount;
			record.RevisedAmount = ps.RevisedAmount;
			record.CuryActualAmount = ps.CuryActualAmount;
			record.ActualAmount = ps.ActualAmount;
			record.CuryDraftCOAmount = ps.CuryDraftChangeOrderAmount;
			record.CuryBudgetedCOAmount = ps.CuryChangeOrderAmount;
			record.BudgetedCOAmount = ps.ChangeOrderAmount;
			record.CuryOriginalCommittedAmount = ps.CuryCommittedOrigAmount;
			record.OriginalCommittedAmount = ps.CommittedOrigAmount;
			record.CuryCommittedCOAmount = ps.CuryCommittedCOAmount;
			record.CommittedCOAmount = ps.CommittedCOAmount;
			record.CuryCommittedAmount = ps.CuryCommittedAmount;
			record.CommittedAmount = ps.CommittedAmount;
			record.CuryCommittedOpenAmount = ps.CuryCommittedOpenAmount;
			record.CommittedOpenAmount = ps.CommittedOpenAmount;
			record.CuryCommittedInvoicedAmount = ps.CuryCommittedInvoicedAmount;
			record.CommittedInvoicedAmount = ps.CommittedInvoicedAmount;

			return record;
		}

		public virtual PMProjectBalanceRecord CreateHeader(string accountType)
		{
			PMProjectBalanceRecord record = new PMProjectBalanceRecord();

			switch (accountType)
			{
				case AccountType.Asset:
					record.RecordID = -10;
					record.AccountGroup = Messages.GetLocal(GL.Messages.Asset);
					break;

				case AccountType.Liability:
					record.RecordID = -20;
					record.AccountGroup = Messages.GetLocal(GL.Messages.Liability);
					break;

				case AccountType.Income:
					record.RecordID = -30;
					record.AccountGroup = Messages.GetLocal(GL.Messages.Income);
					break;

				case AccountType.Expense:
					record.RecordID = -40;
					record.AccountGroup = Messages.GetLocal(GL.Messages.Expense);
					break;

				case PMAccountType.OffBalance:
					record.RecordID = -50;
					record.AccountGroup = Messages.GetLocal(PM.Messages.OffBalance);
					break;
			}

			return record;
		}

		[Obsolete]
		public virtual PMProjectBalanceRecord CreateTotal(string accountType, decimal curyAmount, decimal curyRevisedAmt, decimal curyActualAmt, decimal actualAmt, decimal curyCommittedAmt, decimal curyCommittedOpenAmt, decimal curyCommittedInvoicedAmt, decimal curyCommitteOrigAmt)
		{
			PMProjectBalanceRecord record = new PMProjectBalanceRecord();

			switch (accountType)
			{
				case AccountType.Asset:
					record.RecordID = -11;
					record.Description = Messages.GetLocal(Messages.AssetTotals);
					break;

				case AccountType.Liability:
					record.RecordID = -21;
					record.Description = Messages.GetLocal(Messages.LiabilityTotals);
					break;

				case AccountType.Income:
					record.RecordID = -31;
					record.Description = Messages.GetLocal(Messages.IncomeTotals);
					break;

				case AccountType.Expense:
					record.RecordID = -41;
					record.Description = Messages.GetLocal(Messages.ExpenseTotals);
					break;

				case PMAccountType.OffBalance:
					record.RecordID = -51;
					record.Description = Messages.GetLocal(Messages.OffBalanceTotals);
					break;
			}

			record.CuryAmount = curyAmount;
			record.CuryRevisedAmount = curyRevisedAmt;
			record.CuryActualAmount = curyActualAmt;
			record.ActualAmount = actualAmt;
			record.CuryCommittedAmount = curyCommittedAmt;
			record.CuryCommittedOpenAmount = curyCommittedOpenAmt;
			record.CuryCommittedInvoicedAmount = curyCommittedInvoicedAmt;
			record.CuryOriginalCommittedAmount = curyCommitteOrigAmt;

			return record;
		}

		public virtual PMProjectBalanceRecord CreateTotal(string accountType, decimal curyAmount, decimal curyRevisedAmt, decimal curyActualAmt, decimal actualAmt, decimal curyCommittedAmt, decimal curyCommittedOpenAmt, decimal curyCommittedInvoicedAmt, decimal curyCommitteOrigAmt, decimal curyDraftCOAmt, decimal curyBudgetedCOAmt, decimal curyCommittedCOAmt)
		{
			PMProjectBalanceRecord record = new PMProjectBalanceRecord();

			switch (accountType)
			{
				case AccountType.Asset:
					record.RecordID = -11;
					record.Description = Messages.GetLocal(Messages.AssetTotals);
					break;

				case AccountType.Liability:
					record.RecordID = -21;
					record.Description = Messages.GetLocal(Messages.LiabilityTotals);
					break;

				case AccountType.Income:
					record.RecordID = -31;
					record.Description = Messages.GetLocal(Messages.IncomeTotals);
					break;

				case AccountType.Expense:
					record.RecordID = -41;
					record.Description = Messages.GetLocal(Messages.ExpenseTotals);
					break;

				case PMAccountType.OffBalance:
					record.RecordID = -51;
					record.Description = Messages.GetLocal(Messages.OffBalanceTotals);
					break;
			}

			record.CuryAmount = curyAmount;
			record.CuryRevisedAmount = curyRevisedAmt;
			record.CuryActualAmount = curyActualAmt;
			record.ActualAmount = actualAmt;
			record.CuryCommittedAmount = curyCommittedAmt;
			record.CuryCommittedOpenAmount = curyCommittedOpenAmt;
			record.CuryCommittedInvoicedAmount = curyCommittedInvoicedAmt;
			record.CuryOriginalCommittedAmount = curyCommitteOrigAmt;
			record.CuryDraftCOAmount = curyDraftCOAmt;
			record.CuryBudgetedCOAmount = curyBudgetedCOAmt;
			record.CuryCommittedCOAmount = curyCommittedCOAmt;

			return record;
		}

		public virtual PMProjectBalanceRecord CreateFooter(string accountType)
		{
			PMProjectBalanceRecord record = new PMProjectBalanceRecord();

			switch (accountType)
			{
				case AccountType.Asset:
					record.RecordID = -12;
					break;

				case AccountType.Liability:
					record.RecordID = -22;
					break;

				case AccountType.Income:
					record.RecordID = -32;
					break;

				case AccountType.Expense:
					record.RecordID = -42;
					break;
				case PMAccountType.OffBalance:
					record.RecordID = -52;
					break;
			}

			return record;
		}

		#endregion

		public override int ExecuteDelete(string viewName, IDictionary keys, IDictionary values, params object[] parameters)
		{
			return base.ExecuteDelete(viewName, keys, values, parameters);
		}

		//public override IEnumerable ExecuteSelect(string viewName, object[] parameters, object[] searches, string[] sortcolumns, bool[] descendings, PXFilterRow[] filters, ref int startRow, int maximumRows, ref int totalRows)
		//{
		//	List<object> result = new List<object>();
		//	foreach(var x in base.ExecuteSelect(viewName, parameters, searches, sortcolumns, descendings, filters, ref startRow, maximumRows, ref totalRows))
		//	{
		//		result.Add(x);
		//	}
		//	return result;
		//}

		#region PMImport Implementation
		public bool PrepareImportRow(string viewName, IDictionary keys, IDictionary values)
		{
			if (viewName == nameof(RevenueBudget))
			{
				string accountGroupCD = null;
				if (keys.Contains(nameof(PMRevenueBudget.AccountGroupID)))
				{
					//Import file could be missing the AccountGroupID field and hence the Default value could be set by the DefaultEventHandler

					object keyVal = keys[nameof(PMRevenueBudget.AccountGroupID)];

					if (keyVal is int)
					{
						PMAccountGroup accountGroup = PMAccountGroup.PK.Find(this, (int?) keyVal);
						if (accountGroup != null)
						{
							return accountGroup.Type == GL.AccountType.Income;
						}
					}
					else
					{
						accountGroupCD = (string)keys[nameof(PMRevenueBudget.AccountGroupID)];
					}
				}

				if (!string.IsNullOrEmpty(accountGroupCD))
				{
					PMAccountGroup accountGroup = PXSelect<PMAccountGroup, Where<PMAccountGroup.groupCD, Equal<Required<PMAccountGroup.groupCD>>>>.Select(this, accountGroupCD);
					if (accountGroup != null)
					{
						return accountGroup.Type == GL.AccountType.Income;
					}
				}
				else
				{
					return true;
				}

				return false;

			}
			else if (viewName == nameof(CostBudget))
			{
				string accountGroupCD = null;
				if (keys.Contains(nameof(PMCostBudget.AccountGroupID)))
				{
					accountGroupCD = (string)keys[nameof(PMCostBudget.AccountGroupID)];
				}

				if (!string.IsNullOrEmpty(accountGroupCD))
				{
					PMAccountGroup accountGroup = PXSelect<PMAccountGroup, Where<PMAccountGroup.groupCD, Equal<Required<PMAccountGroup.groupCD>>>>.Select(this, accountGroupCD);
					if (accountGroup != null)
					{
						return accountGroup.IsExpense == true;
					}
				}
				else
				{
					return true;
				}

				return false;
			}
			else if (viewName == nameof(OtherBudget))
			{
				string accountGroupCD = null;
				if (keys.Contains(nameof(PMOtherBudget.AccountGroupID)))
				{
					accountGroupCD = (string)keys[nameof(PMOtherBudget.AccountGroupID)];
				}

				if (!string.IsNullOrEmpty(accountGroupCD))
				{
					PMAccountGroup accountGroup = PXSelect<PMAccountGroup, Where<PMAccountGroup.groupCD, Equal<Required<PMAccountGroup.groupCD>>>>.Select(this, accountGroupCD);
					if (accountGroup != null)
					{
						return accountGroup.IsExpense != true && accountGroup.Type != GL.AccountType.Income;
					}
				}
				else
				{
					return true;
				}

				return false;
			}
			return true;
		}

		public bool RowImporting(string viewName, object row)
		{
			return true;
		}

		public bool RowImported(string viewName, object row, object oldRow)
		{
			return oldRow == null;
		}

		public void PrepareItems(string viewName, IEnumerable items) { }
		#endregion
	}

	public class ChangeProjectID : PXChangeID<PMProject, PMProject.contractCD>
	{
		public ChangeProjectID(PXGraph graph, string name)
			: base(graph, name)
		{
		}

		[PXUIField(DisplayName = "Change ID", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton]
		protected override IEnumerable Handler(PXAdapter adapter)
		{
			string newcd;
			if (adapter.View.Cache.Current != null && adapter.View.Cache.GetStatus(adapter.View.Cache.Current) != PXEntryStatus.Inserted)
			{
				var dialogResult = adapter.View.Cache.Graph.Views[ChangeIdDialogView].AskExt();
				if ((dialogResult == WebDialogResult.OK || (dialogResult == WebDialogResult.Yes && this.Graph.IsExport))
					&& !String.IsNullOrWhiteSpace(newcd = GetNewCD(adapter)))
				{
					ChangeCDProject(adapter.View.Cache, GetOldCD(adapter), newcd, GetType(adapter));
					
					if (adapter.SortColumns != null && adapter.SortColumns.Length > 0 &&
									 String.Equals(adapter.SortColumns[0], typeof(PMProject.contractCD).Name, StringComparison.OrdinalIgnoreCase) &&
									adapter.Searches != null && adapter.Searches.Length > 0)
								{
									adapter.Searches[0] = newcd;
								}
				}
			}

			if (this.Graph.IsContractBasedAPI)
				this.Graph.Actions.PressSave();

			return adapter.Get();
		}
		protected String GetType(PXAdapter adapter) => (string)adapter.View.Cache.GetValue(adapter.View.Cache.Current, typeof(PMProject.baseType).Name);

		public void ChangeCDProject(PXCache cache, string oldCD, string newCD, string type)
			
		{
			System.Collections.Specialized.OrderedDictionary keys =
				new System.Collections.Specialized.OrderedDictionary(StringComparer.OrdinalIgnoreCase)
				{
					{
						typeof(PMProject.contractCD).Name, oldCD
					},
					{
						typeof(PMProject.baseType).Name, type
					}
				};
			System.Collections.Specialized.OrderedDictionary vals =
				new System.Collections.Specialized.OrderedDictionary(StringComparer.OrdinalIgnoreCase)
				{
					{
						typeof(PMProject.contractCD).Name, newCD
					},
					{
						typeof(PMProject.baseType).Name, type
					}
				};
			cache.Update(keys, vals);
		}
	}
}
