// Sample UDF which returns sum of two values.
function StringRight(arg1, arg2) {
    if (arg1 == null) return null;
    var x = String(arg1);
    if (arg2 == null) arg2 = x.length;
    var y = parseInt(arg2);
    if (y >= x.length) return x;
    return x.substr(x.length - y, y);
}