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
where 
eu.INSTREADINGMETHODNUM like  'TST%'
order by eu.RECID desc

select ct.CONTRACTNUM,ct.ATTACHMENTNUM, cust.ACCOUNTNUM, * from EXU_CONTRACTTABLE ct
join EXU_INSTALLATION inst
on inst.INSTNUM= ct.ATTACHMENTNUM
join custtable cust
on cust.ACCOUNTNUM = ct.CUSTACCOUNT
where --inst.INSTREADINGMETHODNUM like  'TS%'
--and
cust.CUSTGROUP like  'TS%'

select REQUESTBODY,* from XAPI_APILogTable
select * from XAPI_APILogValues
where recid > 5637164841
-------------------------------
select * from custgroup where CUSTGROUP like 'Ts%'

select * from XAPI_APIConfigurations


--delete from XAPI_APIConfigurations

--delete from  XAPI_APILogTable where archived = 0

-----=== check in master tables
select * from CUSTTABLE where ACCOUNTNUM = '917602'

select * from  EXU_INSTALLATION where INSTNUM = '19765'

select * from EXU_CONTRACTTABLE where CONTRACTNUM =  '9367872'


--truncate table XAPI_APILogValues

--truncate table XAPI_APILogTable
--drop table XAPI_APILogTable
--drop table XAPI_APILogValues
