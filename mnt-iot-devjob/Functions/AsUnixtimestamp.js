


// Sample UDF which returns sum of two values.
function AsUnixTimestamp(input) {
    var someDate = new Date(input);
    return someDate.getTime() / 1000 | 0;
}

