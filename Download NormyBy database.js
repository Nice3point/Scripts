//wget -A pdf -m -p -E -k -K -np --no-check-certificate -P C:\Users\Roman.LIDER\Desktop\Normy https://normy.by/tnpa/1/

function POSTreq() {
  for (i = 0; i < 88; i++) {
    (function(i){
      var xhr = new XMLHttpRequest();
      var url = "https://normy.by/fond_list.php";
      var params = "codename=&doctype%5B13%5D=on&doctype%5B14%5D=on&doctype%5B5%5D=on&doctype%5B6%5D=on&doctype%5B2%5D=on&doctype%5B9%5D=on&doctype%5B8%5D=on&doctype%5B10%5D=on&doctype%5B4%5D=on&code=&name=&datefrom=&dateto=&block=0&annotation=&kgs=&mks=&mode=false&page="+i;
      xhr.open('POST', url, false);
      xhr.setRequestHeader('Content-type', 'application/x-www-form-urlencoded');
      xhr.onreadystatechange = function () {
      download(xhr.responseText, "data"+i+".json", "text/plain");
      };
      xhr.send(params);
    })(i);
  }
}
function download(data, filename, type) {
  var file = new Blob([data], {type: type});
  if (window.navigator.msSaveOrOpenBlob) // IE10+
    window.navigator.msSaveOrOpenBlob(file, filename);
  else { // Others
    var a = document.createElement("a"),
        url = URL.createObjectURL(file);
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    setTimeout(function() {
      document.body.removeChild(a);
      window.URL.revokeObjectURL(url);
    }, 0);
  }
}
function sleep(miliseconds) {
  var currentTime = new Date().getTime();

  while (currentTime + miliseconds >= new Date().getTime()) {
  }
}
POSTreq();

