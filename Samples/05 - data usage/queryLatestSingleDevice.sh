#!/bin/bash
DEVICEID=EngeTIJ
TABLEURL="https://mntiotroadweather4data.table.core.windows.net/MeasurementsRoadsensorsLatest?sv=2018-03-28&si=public&tn=measurementsroadsensorslatest&sig=47K41Vp9bwj8O1PFQYEAYgZdygoLP8oaiSJfO0UtClA%3D"

FILTER="\$filter=(RowKey%20eq%20'${DEVICEID}')"

HEADER="Accept: application/json;odata=nometadata"
curl -s -H "${HEADER}" "${TABLEURL}&${FILTER}" | jq -r '.value[] | . ' 



