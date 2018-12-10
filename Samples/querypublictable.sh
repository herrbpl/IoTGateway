#!/bin/bash
STATION=amtij
TIME=3600
TABLEURL="https://mntiotroadweather4data.table.core.windows.net/MeasurementsRoadsensorsRaw?sv=2018-03-28&si=MeasurementsRoadsensorsRaw-public&tn=measurementsroadsensorsraw&sig=3VHnSxPL%2F53f1f0PELs7xuHLTW50%2BGGiwXOK%2BhaNV30%3D"

TIMEFILTER=$(expr 2000000000 - $(date +"%s") + $TIME)

FILTER="\$filter=(PartitionKey%20eq%20'${STATION}')%20and%20(RowKey%20lt%20'${TIMEFILTER}')"
HEADER="Accept: application/json;odata=nometadata"

curl -H "${HEADER}" "${TABLEURL}&${FILTER}" | jq -r '.value[] | [ .deviceid, .eventdatetime,.air_temp ] | @csv' 