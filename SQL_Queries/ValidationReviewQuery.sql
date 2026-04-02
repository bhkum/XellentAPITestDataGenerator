-- Customer created 
select CUSTGROUP,* from CUSTTABLE 
where CUSTGROUP = 'TST_CustGr'
order by recid desc 


-- Installation created 
select INSTNUM,INSTREADINGMETHODNUM,* from EXU_INSTALLATION 
where INSTREADINGMETHODNUM like  'TST%'

--where INSTOWNER = '917585'
order by RECID desc



/*
	-- Customer
	917585
	917586

	Contract
	9367878
*/


-- General queries 

select ERR_MSG,* from EGS_MASS_Input
where C006 = '9367906'

--delete from EGS_MASS_Input where C006 = '9367906'

select * from SysAADClientTable

select top 1 name,dv.DISPLAYVALUE

--,*
from DIMENSIONATTRIBUTE da
join DIMENSIONATTRIBUTEVALUE dv
on da.RECID = dv.DIMENSIONATTRIBUTE
where name not like '%System%'
order by da.NAME

select * from XAPI_APIConfigurations