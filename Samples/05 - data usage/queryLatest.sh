#!/bin/bash
TABLEURL="https://mntiotroadweather4data.table.core.windows.net/MeasurementsRoadsensorsLatest?sv=2018-03-28&si=public&tn=measurementsroadsensorslatest&sig=47K41Vp9bwj8O1PFQYEAYgZdygoLP8oaiSJfO0UtClA%3D"

HEADER="Accept: application/json;odata=nometadata"
curl -s -H "${HEADER}" "${TABLEURL}&${SELECT}" | jq -r '.value[] | . ' 



