/*
    The following Postman re-request script will automatically add the relevenat headers to the request, including authorization.

    It requires the environment variables apiToken and apiSecret
*/

const digestMarker = "TCN";
const digestSeperator = ":";
const hashSeperator = "+";

const apiToken = pm.variables.get("apiToken");
const apiSecret = pm.variables.get("apiSecret");

const timestamp = new Date().toISOString();
const httpVerb = pm.request.method.toUpperCase();
const initialPath = "/" + pm.request.url.path.join("/");

const path = pm.request.url.variables.reduce((path, v) => path.replace(":" + v.key, v.value), initialPath);
const url = pm.variables.replaceIn(path).toLowerCase();

const input = httpVerb + hashSeperator + url + hashSeperator + timestamp;
const hash = CryptoJS.HmacSHA256(input, apiSecret);
const digest = CryptoJS.enc.Base64.stringify(hash);
const authToken = digestMarker + " " + apiToken + digestSeperator + digest;

pm.request.headers.add({
    key: "Authorization",
    value: authToken
});

pm.request.headers.add({
    key: "X-Timestamp",
    value: timestamp
});
