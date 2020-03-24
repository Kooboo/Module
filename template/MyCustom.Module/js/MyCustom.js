var client = new Kooboo.HttpClientModel("Custom");
client.executeGet("GetString").then(rsp => {
    document.getElementById("get").innerHTML = rsp.model;
});
