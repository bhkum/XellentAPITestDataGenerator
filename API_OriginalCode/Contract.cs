class EGS_API_newContractAPI
{
    #EXU_API

    EGS_API_DataContract_newContract newContract;
    EXU_ContractTable contractTable;
    EGS_API_DataContract_Response response = new EGS_API_DataContract_Response();
    EXU_UtilityInstallation utilityInstallation;
    EXU_DeliveryGroupTable deliveryGroupTable;
    EXU_BillingPlan exu_billingPlan;
    EXU_ContractTable contractTableTmp;
    EXU_ContractPartTable contractPartTable;
    EXU_InstNum numAllocated;
    boolean okToWrite;

    public EGS_API_DataContract_Response createContract(EGS_API_DataContract_newContract _newContract)
    {
        DataAreaId dataAreaId = _newContract.parmDataAreaId() ? _newContract.parmDataAreaId() :curExt();
        ;        

        changecompany(dataAreaId)
        {
            this.newContract = _newContract;
            return this.doCreateContract();
        }
    }

    boolean agreementHasActiveContract(EXU_AttachmentNum _attachmentNum,
                                       transDate         _transDate)
    {
        return (select firstonly recId
                            from EXU_ContractTable
                           where EXU_ContractTable.AttachmentNum   == _attachmentNum &&
                                 EXU_ContractTable.ContractType    == EXU_ContractType::Agreement &&
                                (EXU_ContractTable.ContractEndDate == dateNull() ||
                                 EXU_ContractTable.ContractEndDate >= _transDate)).recId != 0;
    }

    private void writeData()
    {
        EXU_AgreementTable                  exu_AgreementTable;
        boolean                             createContract = true;
        EXU_ContactPersonRelationManagement contactPersonRelationManagement;


        if (EXU_IsOldLegacyConfiguration_EXU_VAR_Enabled_ReplaceItWherePossible())
        {
            okToWrite = true;
        }
    
    
        if (contractTable.ContractType == EXU_ContractType::Agreement &&
            EXU_DeliveryCategoryTable::EMS_IsDeliveryXellentebIX(contractTable.DeliveryCategory))
        {
            exu_AgreementTable = EXU_AgreementTable::find(contractTable.DeliveryCategory,
                                                          contractTable.CompanyId,
                                                          contractTable.AttachmentNum);
            if (EXU_AgreementTable::useAgreementStatus() &&
                exu_AgreementTable.EMS_AgreementStatusebIX != EMS_AgreementStatusebIX::AcceptedInclBasicData &&
                exu_AgreementTable.EMS_AgreementStatusebIX != EMS_AgreementStatusebIX::MovingInAccepted &&
                exu_AgreementTable.EMS_AgreementStatusebIX != EMS_AgreementStatusebIX::MoveInStandardAccepted &&
                exu_AgreementTable.EMS_AgreementStatusebIX != EMS_AgreementStatusebIX::MoveInOtherAccepted &&
                exu_AgreementTable.EMS_AgreementStatusebIX != EMS_AgreementStatusebIX::MoveInSecondaryAccepted &&
               (exu_AgreementTable.EMS_AgreementStatusebIX != EMS_AgreementStatusebIX::Active ||
                this.agreementHasActiveContract(exu_AgreementTable.AgreementNum, contractTable.ContractStartDate)))
            {
                createContract = checkFailed(strfmt("@EGU6366",
                                                    fieldid2pname(tablenum(EXU_AgreementTable), fieldnum(EXU_AgreementTable, EMS_AgreementStatusebIX)),
                                                    EMS_AgreementStatusebIX::AcceptedInclBasicData,
                                                    EMS_AgreementStatusebIX::MovingInAccepted,
                                                    EMS_AgreementStatusebIX::Active,
                                                    contractTable.ContractStartDate));
            }
    
        }
    
        if (createContract && EXU_Validations::dynamicValidateField(contractTable))
        {
            if (contractTable.validateWrite())
            {
                contractTable.insert();
   
                if (contractTable.ContractType == EXU_ContractType::Agreement &&
                    EXU_DeliveryCategoryTable::EMS_IsDeliveryXellentebIX(contractTable.DeliveryCategory))
                {
                    if(!isConfigurationKeyEnabled(configurationkeynum(EXU_HNGMidtNordDK)) ||
                       EXU_DeliveryCategoryTable::find(contractTable.DeliveryCategory).ChangeSupplierSystem == EXU_ChangeSupplierSystem::XellentebIX) // KPT 2007-12-03
                    {
                        EMS_AgreementTable::setStatus(contractTable.DeliveryCategory,
                                                      contractTable.CompanyId,
                                                      contractTable.AttachmentNum,
                                                      EMS_AgreementStatusebIX::Active);
                    }
                }

                if(isConfigurationkeyEnabled(configurationkeynum(EGS_AdditionalProduct)) && contractTable.ContractType == EXU_ContractType::Agreement)
                {
                    EGS_AdditionalProductAgreement::createContractPartAdditionalProduct(contractTable.AttachmentNum,
                                                                                        contractTable.DeliveryCategory,
                                                                                        contractTable.CompanyId);
                }

                response.parmStatus(#ResponseCode_OK);
                response.parmMessage(contractTable.ContractNum);
                //clear infolog after successful operation - it may contain errors from contract line creation attempt
                infolog.clear();
            }
            else
            {
                this.cancelNewContract();
                response.parmStatus(#ResponseCode_NOT_OK);
                response.parmMessage(EXU_Infolog::get().copyLastInfologText());
            }
        }
        else
        {
            this.cancelNewContract();
            response.parmStatus(#ResponseCode_NOT_OK);
            response.parmMessage(EXU_Infolog::get().copyLastInfologText());
        }
    }

    private void cancelNewContract()
    {
        if (EXU_IsOldLegacyConfiguration_EXU_VAR_Enabled_ReplaceItWherePossible())
        {
          
            if (utilityInstallation.isMicroProduction())
            {
                EXU_TH_EXU_UnitHolderPeriod::deleteAllOnUtilInstallation(utilityInstallation.DeliveryCategory, utilityInstallation.InstNum);
            }
        }
    
    }

    private void setDates()
    {
        EXU_LatestStatementDate latestStatementDate;
        EXU_FinancialPlanTable  exu_FinancialPlanTable;
        ;

        contractTable.ContractStartDate      = contractTableTmp.ContractStartDate;
        contractTable.ContractEndDate        = contractTableTmp.ContractEndDate;            
            
        // De næste linier ligger også i modified på BillingPlanNum feltet
        latestStatementDate = EXU_ContractTable::findLatestStatementDate(contractTable);
            
        if (latestStatementDate == DateNull() ||
                    latestStatementDate  < contractTable.ContractStartDate)
        {
            latestStatementDate  = contractTable.ContractStartDate;
        }
            
        contractTable.InitFromBillingPlan(contractTable.BillingPlanNum);
        exu_BillingPlan                          = EXU_BillingPlan::find(contractTable.BillingPlanNum);
        contractTable.NextBillingEndDate     = EXU_contractTable::CalcNextBillingEndDate(latestStatementDate,
                                                                                         exu_BillingPlan.BillingInterval,
                                                                                         contractTable.BillingDates,
                                                                                         contractTable.BillingWeekday,
                                                                                         contractTable.BillingEvenOddWeeks,
                                                                                         exu_BillingPlan.ContractMinNumberOfDays);

        if (isConfigurationkeyEnabled(configurationkeynum(EXU_OeE)) &&
                    exu_BillingPlan::NextBillingEndDate(contractTable))
        {
            contractTable.NextBillingEndDate = exu_BillingPlan::NextBillingEndDate(contractTable);
        }
            
        if (contractTable.ContractType == EXU_ContractType::Financing)
        {
            select firstonly exu_FinancialPlanTable
                            order by FinancialNum, FinancialPeriodNum
                               where exu_FinancialPlanTable.FinancialNum == contractTable.AttachmentNum &&
                                     exu_FinancialPlanTable.Status_Fee   == EXU_FinancialPlanStatus::Created;
            
            contractTable.NextBillingEndDate = exu_FinancialPlanTable.FinanciaDueDate;
        }
            
        contractTable.NextBillingIncludeDate = EXU_ContractTable::CalcNextBillingIncludeDate(exu_BillingPlan, contractTable.NextBillingEndDate);
    }

    private void initFromContractTemplate()
    {
        EXU_LatestStatementDate latestStatementDate;
        EXU_FinancialPlanTable  exu_FinancialPlanTable;

        contractTable.initFromContractTemplate(contractTable.ContractTemplateNum);

        if (contractTable.AttachmentNum && deliveryGroupTable.DeliveryGroupNum && deliveryGroupTable.BillingCycle)
        {
            contractTable.BillingPlanNum = deliveryGroupTable.BillingCycle;
        }

        contractTable.initFromBillingPlan(contractTable.BillingPlanNum);
            
        latestStatementDate = EXU_contractTable::findLatestStatementDate(contractTable);
            
        if (latestStatementDate == DateNull() ||
                    latestStatementDate  < contractTable.ContractStartDate)
        {
            latestStatementDate  = contractTable.ContractStartDate;
        }
            
        exu_BillingPlan                          = EXU_BillingPlan::find(contractTable.BillingPlanNum);
        contractTable.NextBillingEndDate     = EXU_contractTable::CalcNextBillingEndDate(latestStatementDate,
                                                                                         exu_BillingPlan.BillingInterval,
                                                                                         contractTable.BillingDates,
                                                                                         contractTable.BillingWeekday,
                                                                                         contractTable.BillingEvenOddWeeks,
                                                                                         exu_BillingPlan.ContractMinNumberOfDays);

        if (isConfigurationkeyEnabled(configurationkeynum(EXU_OeE)) &&
                    exu_BillingPlan::NextBillingEndDate(contractTable))
        {
            contractTable.NextBillingEndDate = exu_BillingPlan::NextBillingEndDate(contractTable);
        }

            
        if (contractTable.ContractType == EXU_ContractType::Financing)
        {
            select firstonly exu_FinancialPlanTable
                            order by FinancialNum, FinancialPeriodNum
                               where exu_FinancialPlanTable.FinancialNum == contractTable.AttachmentNum &&
                                     exu_FinancialPlanTable.Status_Fee   == EXU_FinancialPlanStatus::Created;
            
            contractTable.NextBillingEndDate = exu_FinancialPlanTable.FinanciaDueDate;
        }
            
        contractTable.NextBillingIncludeDate = EXU_ContractTable::CalcNextBillingIncludeDate(exu_BillingPlan, contractTable.NextBillingEndDate);
            
            
        if (contractTable.ContractType != contractTableTmp.ContractType)
        {
            contractTable.AttachmentNum = "";
            contractTable.CompanyId = "";
            contractTable.DeliveryCategory = "";
        }
            
        if (contractTable.ContractType      == EXU_ContractType::Installation &&
                    contractTable.ContractStartDate != dateNull())
        {

            latestStatementDate = EXU_ContractTable::findLatestStatementDate(contractTable);
            
            contractTableTmp.InitFromBillingPlan(contractTableTmp.BillingPlanNum);
            
            exu_BillingPlan                         = EXU_BillingPlan::find(contractTableTmp.BillingPlanNum);
            contractTableTmp.NextBillingEndDate = EXU_ContractTable::CalcNextBillingEndDate(latestStatementDate,
                                                                                            exu_BillingPlan.BillingInterval,
                                                                                            contractTableTmp.BillingDates,
                                                                                            contractTableTmp.BillingWeekday,
                                                                                            contractTableTmp.BillingEvenOddWeeks,
                                                                                            exu_BillingPlan.ContractMinNumberOfDays);
            
            contractTableTmp.NextBillingIncludeDate = EXU_ContractTable::CalcNextBillingIncludeDate(exu_BillingPlan, contractTableTmp.NextBillingEndDate);
        }
    }

    private EXU_ContractNum generateContractNum()
    {
        NumberSeq numberSeq;
        ;
        if (EXU_BillingParameters::find().ContractModulo11Check == NoYes::Yes)
        {
            while (modulo11(ContractTable.ContractNum) != 1 ||
                       ContractTable.ContractNum == "")
            {
                numberSeq = NumberSeq::newGetNum(EXU_BillingParameters::numRef_EXU_ContractNum(), true,true);
        
                if (numberSeq)
                {
                    ContractTable.ContractNum = numberSeq.num();
                    numAllocated                  = ContractTable.ContractNum;
                }
            }
        }
        else
        {
            numberSeq = NumberSeq::newGetNum(EXU_BillingParameters::numRef_EXU_ContractNum(), true,true);
        
            if (numberSeq)
            {
                ContractTable.ContractNum = numberSeq.num();
                numAllocated                  = ContractTable.ContractNum;
            }
        }

        return contractTable.ContractNum;
    }

    private boolean initData()
    {
        EXU_ContractNum contractNum;
        

        EXU_ContractTemplate exu_contractTemplate;
        EXU_FinancialPlanTable exu_FinancialPlanTable;
        
        
        EXU_ContractTable contractTableMicroProd;
        CustTable custTableMicroProd;

        try
        {
            if (!newContract.parmContractNum())
                contractNum = this.generateContractNum();
            else
                contractNum = newContract.parmContractNum();

            contractTableTmp.ContractNum = contractNum;
            contractTableTmp.CustAccount = newContract.parmCustAccount();
            contractTableTmp.ContractType = EXU_Global::enumSymbol2Value(enumNum(EXU_ContractType), newContract.parmContractType());
            contractTableTmp.AttachmentNum = newContract.parmAttachmentNum();
            contractTableTmp.CompanyId = newContract.parmCompanyId();
            contractTableTmp.ContractTemplateNum = newContract.parmContractTemplateNum();
            contractTableTmp.DeliveryCategory = newContract.parmDeliveryCategory();
            contractTableTmp.ContractStartDate = newContract.parmContractStartDate();
            contractTableTmp.ContractEndDate = newContract.parmContractEndDate() ? str2Date(newContract.parmContractEndDate(), 321) : dateNull();
            contractTableTmp.NextBillingIncludeDate = newContract.parmNextBillingEndDate();
            contractTableTmp.BillingPlanNum = newContract.parmBillingPlanNum();

            switch (contractTableTmp.ContractType)
            {
                case EXU_ContractType::Agreement :
                {
                    contractTableTmp.initFromAgreementTable(contractTableTmp.DeliveryCategory,
                                                                contractTableTmp.CompanyId,
                                                                contractTableTmp.AttachmentNum);
        
                    exu_BillingPlan                             = EXU_BillingPlan::find(contractTableTmp.BillingPlanNum);
                    contractTableTmp.initFromBillingPlan(contractTable.BillingPlanNum);
        
                    contractTableTmp.NextBillingEndDate     = EXU_contractTable::CalcNextBillingEndDate(contractTableTmp.ContractStartDate,
                                                                                                        exu_BillingPlan.BillingInterval,
                                                                                                        contractTableTmp.BillingDates,
                                                                                                        contractTableTmp.BillingWeekday,
                                                                                                        contractTableTmp.BillingEvenOddWeeks,
                                                                                                        exu_BillingPlan.ContractMinNumberOfDays);
        
                    if (isConfigurationkeyEnabled(configurationkeynum(EXU_OeE)) &&
                            exu_BillingPlan::NextBillingEndDate(contractTable))
                    {
                        contractTableTmp.NextBillingEndDate = exu_BillingPlan::NextBillingEndDate(contractTable);
                    }

                    contractTableTmp.NextBillingIncludeDate = EXU_contractTable::CalcNextBillingIncludeDate(exu_BillingPlan, contractTableTmp.NextBillingEndDate);
        
                    exu_ContractTemplate = EXU_ContractTemplate::find(contractTableTmp.ContractTemplateNum);
        
                    contractTableTmp.MaxDaysAfterDate       = exu_ContractTemplate.MaxDaysAfterDate;
                    contractTableTmp.MaxDaysBeforeDate      = exu_ContractTemplate.MaxDaysBeforeDate;

                    if(EXU_IsOldLegacyConfiguration_EXU_VAR_Enabled_ReplaceItWherePossible())
                    {
                        contractTableTmp.BillingDatesMeterFree  = exu_ContractTemplate.BillingDatesMeterFree;
                    }
        
                    break;
                }
        
                case EXU_ContractType::Financing :
                {
                    contractTableTmp.initFromFinancialTable(contractTableTmp.AttachmentNum,
                                                                    contractTableTmp.CompanyId,
                                                                    contractTableTmp.DeliveryCategory);
        
                    exu_BillingPlan                             = EXU_BillingPlan::find(contractTableTmp.BillingPlanNum);
                    contractTableTmp.initFromBillingPlan(contractTable.BillingPlanNum);
        
                    select firstonly exu_FinancialPlanTable
                                order by FinancialNum, FinancialPeriodNum
                                   where exu_FinancialPlanTable.FinancialNum == contractTableTmp.AttachmentNum &&
                                         exu_FinancialPlanTable.Status_Fee   == EXU_FinancialPlanStatus::Created;
        
                    contractTableTmp.NextBillingEndDate     = exu_FinancialPlanTable.FinanciaDueDate;
                    contractTableTmp.NextBillingIncludeDate = EXU_ContractTable::CalcNextBillingIncludeDate(exu_BillingPlan, contractTableTmp.NextBillingEndDate);
        
                    break;
                }
                
                case EXU_ContractType::Installation :
                {
                    if (EXU_IsOldLegacyConfiguration_EXU_VAR_Enabled_ReplaceItWherePossible())
                    {
                        contractTableTmp.DeliveryCategory = '';
                    }
                    select firstonly utilityInstallation
                                    where contractTableTmp.AttachmentNum == utilityInstallation.InstNum;

                    if (utilityInstallation)
                    {

                        select firstonly deliveryGroupTable
                                    where utilityInstallation.DeliveryGroupNum_UtilityArea == deliveryGroupTable.DeliveryGroupNum
                                        && deliveryGroupTable.DeliveryGroupCode == EXU_DeliveryGroupCode::UtilityArea;
                                
                        contractTableTmp.BillingPlanNum = deliveryGroupTable.BillingCycle;
                    }
                    break;
                }                   
            }
        
            //contractTable.initFromContractTemplate(contractTableTmp.ContractTemplateNum);
            

            contractTable.ContractNum            = contractTableTmp.ContractNum;
            contractTable.AttachmentNum          = contractTableTmp.AttachmentNum;
            contractTable.DeliveryCategory       = contractTableTmp.DeliveryCategory;
            contractTable.CompanyId              = contractTableTmp.CompanyId;
            contractTable.ContractType           = contractTableTmp.ContractType;
            contractTable.CustAccount            = contractTableTmp.CustAccount;

            if (contractTableTmp.ContractType == EXU_ContractType::Agreement ||
                contractTableTmp.ContractType ==  EXU_ContractType::Financing)
            {
                contractTable.initFromCustTable(contractTable.CustAccount);
            }

            
            contractTable.ContractTemplateNum    = contractTableTmp.ContractTemplateNum;
            contractTable.BillingPlanNum         = contractTableTmp.BillingPlanNum;
            contractTable.ContractCategoryId1    = contractTableTmp.ContractCategoryId1;
            this.initFromContractTemplate();
            this.setDates();            

            contractTable.PrintWellcomeLetter    =  EXU_ContractTemplate::find(contractTableTmp.ContractTemplateNum).UseWelcomeLetter;

            if(EXU_IsOldLegacyConfiguration_EXU_VAR_Enabled_ReplaceItWherePossible())
            {
                contractTable.EGS_PriceSettingMainContractNum = contractTableTmp.EGS_PriceSettingMainContractNum;
            }

            if (contractTableTmp.ResponsibleWorker != 0 )
            {
                contractTable.ResponsibleWorker = contractTableTmp.ResponsibleWorker;
            }

            contractTable.IgnoreParentBusinessRelation = contractTableTmp.IgnoreParentBusinessRelation;
        
            if (EXU_IsOldLegacyConfiguration_EXU_VAR_Enabled_ReplaceItWherePossible())
            {
                if (isConfigurationKeyEnabled(configurationkeynum(EGS_MicroProduction)) &&
                    contractTableTmp.ContractType == EXU_ContractType::Installation)
                {
                    utilityInstallation = EXU_UtilityInstallation::find(contractTable.DeliveryCategory, contractTable.AttachmentNum);
                    contractTableMicroProd = EXU_TH_EXU_UtilityInstallation::construct().getAnyContractOnInstallationEl(utilityInstallation);
                    if (contractTableMicroProd.RecId)
                    {
                        custTableMicroProd = CustTable::find(contractTableMicroProd.CustAccount);
        
                        contractTable.CustAccount = custTableMicroProd.AccountNum;
                        contractTable.initFromCustTable(custTableMicroProd.AccountNum);
                        contractTable.eInvoiceEANNum = custTableMicroProd.EinvoiceEANNum;
                        contractTable.eInvoiceAccountCode = contractTableMicroProd.eInvoiceAccountCode;
                    }
                }
            }
            return true;
        }
        catch
        {
            response.parmStatus(#ResponseCode_NOT_OK);
            response.parmMessage(EXU_Infolog::get().copyLastInfologText());
            return false;
        }
    }

    /// <summary>
    /// this method updates start/end date of already existing contract line
    /// we assume that there is only type of product per line for new contract
    /// </summary>
    private void updateContractLine()
    {
        EXU_ContractPartTable contractPartForUpdate;
        EXU_ProductNum productNum = newContract.parmContractPart().parmProductNum();
        EXU_UtilityInstallation utilityInst = EXU_UtilityInstallation::find(newContract.parmDeliveryCategory(), newContract.parmAttachmentNum());
        
        select firstonly forupdate contractPartForUpdate where contractPartForUpdate.ContractNum == contractTable.ContractNum
            && contractPartForUpdate.ProductNum == productNum;
        
        if (contractPartForUpdate)
        {
            EXU_Date startDate = newContract.parmContractPart().parmContractPartStartDate();
            EXU_Date endDate = newContract.parmContractPart().parmContractPartEndDate() ? str2Date(newContract.parmContractPart().parmContractPartEndDate(), 321) : dateNull();
            contractPartForUpdate.Startdate = contractPartForUpdate.Startdate > startDate  ? contractPartForUpdate.Startdate : startDate;
            
            if (contractPartForUpdate.EndDate)
            {
                contractPartForUpdate.EndDate = contractPartForUpdate.EndDate < endDate ? contractPartForUpdate.EndDate : endDate;
            }
            else
                contractPartForUpdate.EndDate = endDate;

            //if utility is found update the delivery category of the installation
            if (utilityInst)
                contractPartForUpdate.DeliveryCategory = utilityInst.DeliveryCategory;            
 
            contractPartForUpdate.doUpdate();
        }
    }

    /// <summary>
    /// remove already created lines by standard mechanism (from template)
    /// </summary>
    private void removeContractLines()
    {
        EXU_ContractPartTable partTable;
        ttsbegin;
        partTable = EXU_ContractPartTable::findAllByDate(contractTable.ContractStartDate, contractTable.ContractNum, true);
        while (partTable)
        {
            partTable.delete();
            next partTable;
        }
        ttscommit;
    }

    /// <summary>
    /// create new contract line using product from the request first by removing existing lines
    /// created by standard mechanism and then updadint the dates - standard method does not handle
    /// contract line start/end date properly although they are provided in the input params
    /// </summary>
    private void createContractLine()
    {
        this.removeContractLines();

        contractTable.createContractParts(false, newContract.parmContractPart().parmContractPartStartDate(), "", "", 
            newContract.parmContractPart().parmProductNum(), "", "", newContract.parmContractPart().parmContractPartStartDate(), 
            str2Date(newContract.parmContractPart().parmContractPartEndDate(), 321));

        this.updateContractLine();
    }

    private EGS_API_DataContract_Response doCreateContract()
    {   
        ttsbegin;

        if (this.initData())
        {
            this.writeData();
            this.createContractLine();
        }

        if (response.parmStatus() == #ResponseCode_OK)
            ttscommit;
        else 
            ttsabort;

        return response;
    }

}