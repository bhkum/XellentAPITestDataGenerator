-- Customer created 
select CUSTGROUP,ACCOUNTNUM,* from CUSTTABLE 
where CUSTGROUP like 'TST%'
order by recid desc 

-- Installation created 
select INSTNUM,INSTREADINGMETHODNUM,* from EXU_INSTALLATION 
where INSTREADINGMETHODNUM like  'TST%'
order by RECID desc

select eu.INSTNUM,uinst.DELIVERYCATEGORY from EXU_INSTALLATION eu
join EXU_UTILITYINSTALLATION uinst
on uinst.INSTNUM = eu.INSTNUM
where eu.INSTREADINGMETHODNUM like  'TST%'
order by eu.RECID desc

select ct.CONTRACTNUM,ct.ATTACHMENTNUM, cust.ACCOUNTNUM, * from EXU_CONTRACTTABLE ct
join EXU_INSTALLATION inst
on inst.INSTNUM= ct.ATTACHMENTNUM
join custtable cust
on cust.ACCOUNTNUM = ct.CUSTACCOUNT
where inst.INSTREADINGMETHODNUM like  'TST%'
and cust.CUSTGROUP like  'TST%'

select * from XAPI_APILogTable
-------------------------------
select * from custgroup where CUSTGROUP like 'Ts%'

select * from XAPI_APIConfigurations