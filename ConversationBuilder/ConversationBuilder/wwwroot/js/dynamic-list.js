// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

var idCounter = 1;
var listArray = ['0']; 


function AddToList() {
    if(document.getElementById('deleteProperty-'.concat(idCounter-1)) !== null) {
        document.getElementById('deleteProperty-'.concat(idCounter - 1)).style.visibility = 'visible';
    }
    var newProperty = GenerateNewListProperty(idCounter);
    document.getElementById('otherProperties').insertAdjacentHTML('beforeend',newProperty);
    listArray.push(idCounter.toString());
    idCounter = idCounter +1 ;
}

function RemoveFromList(id) {
    //from id extract the number
    var propId = id.toString().split('-')[1];
    document.getElementById('otherProperty-'.concat(propId)).remove();
    var index = listArray.indexOf(propId.toString(),0);
    if(index > -1){
        listArray.splice(index,1);
    }
}

function GetValueInputBoxes() {
    var otherProps = [];
   for (i =0; i < listArray.length; i++){
       var value = GetElement('otherPropertyValue-'.concat(listArray[i])).value;
       otherProps.push(value);
   }  
   return otherProps;
}


function GenerateNewListProperty (counter) {
     var rawOtherProperty = ' <div id="otherProperty">\n' +
         '                <input id="otherPropertyValue" name="propertyValue" type="text" placeholder="Property value"/>\n' +
         '                <i id="deleteProperty" class="btn btn-default" onclick="RemoveFromList(id)">DELETE</i>\n' +
         '                 </ br> </ br>'
         '            </div>';
     var freshProp = new DOMParser().parseFromString(rawOtherProperty,"text/html");
     freshProp.getElementById("otherProperty").id = 'otherProperty-'.concat(counter);
    freshProp.getElementById("deleteProperty").id = 'deleteProperty-'.concat(counter);
    freshProp.getElementById("otherPropertyValue").id = 'otherPropertyValue-'.concat(counter);

     return freshProp.body.innerHTML;   
}