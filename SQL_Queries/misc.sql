select ERR_MSG,* from EGS_MASS_Input
where C006 = '9367906'

--delete from EGS_MASS_Input where C006 = '9367906'

select top 1 name,dv.DISPLAYVALUE

--,*
from DIMENSIONATTRIBUTE da
join DIMENSIONATTRIBUTEVALUE dv
on da.RECID = dv.DIMENSIONATTRIBUTE
where name not like '%System%'
order by da.NAME
