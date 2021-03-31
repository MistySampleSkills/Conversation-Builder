// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

var idCounter = 1;
var keyValueArray = ['0']; 


function AddToDictionary() {
    if(document.getElementById('deleteProperty-'.concat(idCounter-1)) !== null) {
        document.getElementById('deleteProperty-'.concat(idCounter - 1)).style.visibility = 'visible';
    }
    var newProperty = GenerateNewListProperty(idCounter);
    document.getElementById('otherProperties').insertAdjacentHTML('beforeend',newProperty);
    keyValueArray.push(idCounter.toString());
    idCounter = idCounter +1 ;
}

function RemoveFromDictionary(id) {
    //from id extract the number
    var propId = id.toString().split('-')[1];
    document.getElementById('otherProperty-'.concat(propId)).remove();
    var index = keyValueArray.indexOf(propId.toString(),0);
    if(index > -1){
        keyValueArray.splice(index,1);
    }
}

function KeyValuePair(key,value) {
    this.key = key;
    this.value = value;   
}

function GetKeyValueInputBoxes() {
    var otherProps = [];
   for (i =0; i < keyValueArray.length; i++){
       var key = GetElement('otherPropertyKey-'.concat(keyValueArray[i])).value;
       var value = GetElement('otherPropertyValue-'.concat(keyValueArray[i])).value;
       var kvp = new KeyValuePair(key,value);
       otherProps.push(kvp);
   }  
   return otherProps;
}


function GenerateNewDictionaryProperty (counter) {
     var rawOtherProperty = ' <div id="otherProperty">\n' +
         '                <input id="otherPropertyKey" name="propertyKey" type="text" placeholder="Property name"/>\n' +
         '                <input id="otherPropertyValue" name="propertyValue" type="text" placeholder="Property value"/>\n' +
         '                <i id="deleteProperty" class="btn btn-default" onclick="RemoveFromDictionary(id)">DELETE</i>\n' +
         '                 </ br> </ br>'
         '            </div>';
     var freshProp = new DOMParser().parseFromString(rawOtherProperty,"text/html");
     freshProp.getElementById("otherProperty").id = 'otherProperty-'.concat(counter);
    freshProp.getElementById("deleteProperty").id = 'deleteProperty-'.concat(counter);
    freshProp.getElementById("otherPropertyKey").id = 'otherPropertyKey-'.concat(counter);
    freshProp.getElementById("otherPropertyValue").id = 'otherPropertyValue-'.concat(counter);

     return freshProp.body.innerHTML;   
}