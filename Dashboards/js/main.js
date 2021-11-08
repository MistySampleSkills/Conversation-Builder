var ip;
var connectedFlag = false;
var fetchClient;
var lightSocket;
var uploadBox = $('.box');
var filesInput = uploadBox.find('input[type="file"]');
var selectedFiles;
var runningSkills = {};
var currentSkillName;
var skillIds = {};

$('[data-toggle="tooltip"]').tooltip();

$(".new-window-tab").hover(

	function () {
		$(this).find("img").attr("src", "img/new-window-black.svg");
	},
	function () {
		$(this).find("img").attr("src", "img/new-window.svg");
	}
);

var ipInStorage = sessionStorage.getItem("ip");
ip = ipInStorage ? ipInStorage : "";

if (ip !== "") {
	ConnectToRobotMain();
}

// CONNECT
$("#connect").submit(function (e) {

	e.preventDefault();

	if ($("#connect-button").hasClass("connect")) {

		ip = $("#ip-address").val();

		if (!ip) {
			showToastMessage("Must enter IP address or hostname.");
			ReEnableConnectionButton();
			return;
		}

		sessionStorage.setItem("ip", ip);
		ConnectToRobotMain();
	}
	else {

		DisconnectFromRobot();
	}
});

function ConnectToRobotMain() {

	showToastMessage("Attempting to connect to Misty...", 20000);

	$("#connect-button").html("Connecting...");
	$("#ip-address").prop("disabled", true);
	$("#connect-button").prop("disabled", true);

	try {
		fetchClient = new FetchClient(ip, 10000);
		fetchClient.GetCommand("device", function (data) {

			showToastMessage("Connected successfully.");
			$("#ip-address").val(ip);

			$("#connect-button").html("Disconnect");
			$("#connect-button").removeClass("connect");
			$("#connect-button").prop("disabled", false);
			$("#ip-address").prop("disabled", true);

		}, null, InitialCallToMistyFailed);
	}
	catch {
		showToastMessage("Unable to connect to Misty!", 10000);
		ReEnableConnectionButton();
		return;
	}
}

function InitialCallToMistyFailed(request, status, err) {

	showToastMessage("Unable to connect to Misty!", 10000);
	console.log("Call failed: " + err);
	ReEnableConnectionButton();
}

function ReEnableConnectionButton() {

	$("#connect-button").html("Connect");
	$("#connect-button").addClass("connect");
	$("#ip-address").prop("disabled", false);
	$("#connect-button").prop("disabled", false);
}

function getSkillsCallback(data) {

	$("#ip-address-button").html("Connected");

	console.log("Retrieving skills ... ");
	var result = data.result;

	$(".rTable").empty();

	result.sort(function (a, b) {
		if (a.name < b.name) { return -1; }
		if (a.name > b.name) { return 1; }
		return 0;
	});

	for (var i in result) {

		var name = result[i].name;
		skillIds[name] = result[i].uniqueId;

		var trashcanImage = document.createElement("img");
		trashcanImage.src = "img/trash.svg";
		trashcanImage.className = "icon";
		trashcanImage.alt = "delete icon";

		var button = document.createElement("button");
		button.className = "btn start";
		button.textContent = "Start";
		button.onclick = startStopSkill;

		var settingsImage = document.createElement("img");
		settingsImage.src = "img/gear.svg";
		settingsImage.className = "icon filter-purple";
		settingsImage.alt = "advanced settings icon";

		var uninstall = document.createElement("div");
		uninstall.className = "rTableCell skill-uninstall";
		uninstall.append(trashcanImage);
		uninstall.onclick = deleteSkillFromRobot;

		var skillName = document.createElement("div");
		skillName.className = "rTableCell skill-name";
		skillName.textContent = name;

		var start = document.createElement("div");
		start.className = "rTableCell skill-start";
		start.append(button);

		var advancedSettings = document.createElement("div");
		advancedSettings.className = "rTableCell skill-advanced";
		advancedSettings.append(settingsImage);
		advancedSettings.onclick = showAdvancedSettings;

		var skill = document.createElement('li');
		skill.id = name;
		skill.className = "rTableRow";

		skill.append(uninstall);
		skill.append(skillName);
		skill.append(start);
		skill.append(advancedSettings);

		$(".rTable").append(skill);
	}

	console.log(Object.keys(skillIds));
}

async function connectToSocket() {

	connectedFlag = false;

	if (lightSocket) {
		lightSocket.Disconnect();
	}

	lightSocket = new LightSocket(ip, socketOpenCallback);

	$("#ip-address-button").html("Connecting...");
	$("#ip-address-button").prop("disabled", true);
	$("#ip-address").prop("disabled", true);

	lightSocket.Connect();
}

function socketOpenCallback() {

	//random id so we don't run into subs with the same name
	var id = Math.floor((Math.random() * 1000000) + 1);

	lightSocket.Subscribe("SkillRunner" + id + "-SelfState", "SelfState", 500, null, null, null, null, function (data) {
		connectedFlag = true;
	});

	lightSocket.Subscribe("SkillRunner" + id + "-SkillData", "SkillData", 0, null, null, null, null, skillDataCallback);

	$("#ip-address").prop("disabled", false);
	$("#ip-address-button").prop("disabled", false);
}

function skillDataCallback(data) {

	var printDetails = true;

	if (data === null || data === undefined || data.message === null || data.message === undefined || (data.message.data === undefined && data.message.message === undefined)) {
		printDetails = false;
	}

	var objectData = data.message.data === undefined || data.message.data === null ? data.message : data.message.data;
	var messageString = data.message.message === undefined || data.message.message === null ? "" : data.message.message;

	if (messageString.includes("Failed ") ||
		messageString.includes("Compile error") ||
		messageString.includes("Failed ") ||
		messageString.includes("Script error")) {
		//Error
		console.log(("%c" + data.message.message + " " + data.message.timestamp), "color:#FF0000");
	}
	else if (messageString.includes("completed")) {
		fetchClient.GetCommand("skills/running", getRunningSkillsCallback);
	}
	else if (messageString.includes("Calling command")) {
		//From Verbose BroadcaseMode
		console.log(JSON.stringify("%c" + data.message.message + " " + data.message.timestamp), "color:#008000");
	}
	else if (messageString.includes("Debug:")) {
		//From Verbose User Debug Statement and in Verbose, Debug, or All BroadcastMode
		console.log(JSON.stringify("%c" + data.message.message + " " + data.message.timestamp), "color:#0000FF");
	}
	else if (printDetails) {
		console.log(JSON.stringify("%c" + data.message.message + " " + data.message.timestamp), "color:#FFA500");
	}

	try {
		console.log(JSON.parse(objectData));
	}
	catch{
		console.log(objectData);
	}
}

// INSTALL
filesInput.on('change', function (e) {
	selectedFiles = e.target.files;
	uploadBox.trigger('drop');
});

uploadBox.on('drag dragstart dragend dragover dragenter dragleave drop submit', function (e) {
	e.preventDefault();
	e.stopPropagation();
})
	.on('dragover dragenter', function () {
		uploadBox.addClass('is-dragover');
	})
	.on('dragleave dragend drop', function () {
		uploadBox.removeClass('is-dragover');
	})
	.on('drop submit', function (e) {
		droppedFiles = selectedFiles ? selectedFiles : e.originalEvent.dataTransfer.files;
		uploadFilesToRobot();
	});

function uploadFilesToRobot() {

	var jsonFileIncluded = false;
	var thisIsAlreadyAZipFile = false;
	var zip = new JSZip();

	if (droppedFiles) {

		$.each(droppedFiles, function (i, file) {
			if (file.name.indexOf("json") > -1) {
				jsonFileIncluded = true;
			}
			zip.file(file.name, file);
		});
	}

	if (droppedFiles && droppedFiles.length == 1 && droppedFiles[0].name.indexOf("zip") > -1) {

		jsonFileIncluded = true;
		thisIsAlreadyAZipFile = true;
	}

	if (!jsonFileIncluded) {
		showToastMessage("Skill file uploads must include a json meta file.", 5000);
		return;
	}

	var xhr = new XMLHttpRequest();
	xhr.addEventListener('load', function (event) {
		var response = JSON.parse(event.target.response);
		console.log("Skill command response: " + JSON.stringify(response));

		if (response.status == "Success") {
			showToastMessage("Files successfully uploaded.", 5000);
			fetchClient.PostCommand("led", "{\"Red\":0, \"Green\":255, \"Blue\":0}", null);
			fetchClient.GetCommand("skills", getSkillsCallback);
		}
		else {
			showToastMessage("Failed to upload skill files.", 5000);
			fetchClient.PostCommand("led", "{\"Red\":255, \"Green\":0, \"Blue\":0}", null);
		}
	});

	xhr.open('POST', 'http://' + ip + '/api/skills');

	if (thisIsAlreadyAZipFile) {

		var data = new FormData();
		data.append("File", droppedFiles[0]);
		data.append("OverwriteExisting", true);

		xhr.send(data);

	} else {

		zip.generateAsync({ type: "blob" })
			.then(function (content) {

				var data = new FormData();
				data.append("File", content);
				data.append("OverwriteExisting", true);
				return data;
			})
			.then(function (data) {
				xhr.send(data);
			});
	}	
}

// GENERATE
$("#generate-meta-template").submit(function (e) {
	e.preventDefault();
	var skillName = $("#generate-template-skill-name").val();

	if (!skillName) {
		showToastMessage("Please enter the name of the new skill.");
		return;
	}

	$("#generate-template-skill-name").val("");

	showToastMessage("Generating meta template for '" + skillName + "'...");

	//create template with user-supplied skill name and auto-generated UUID
	var template = '{\n\t"Name": "' + skillName + '",\n\t"UniqueId" : "' + uuidv4() + '",\n\t"Description": "My skill is amazing!",\n\t"StartupRules": ["Manual", "Robot"],\n\t"Language": "javascript",\n\t"BroadcastMode": "verbose",\n\t"TimeoutInSeconds": 300,\n\t"CleanupOnCancel": false,\n\t"WriteToLog": false,\n\t"Parameters": {\n\t\t"int":10,\n\t\t"double":20.5,\n\t\t"string":"twenty"\n\t}\n}';

	if ($('#display').prop('checked')) {

		//replace code for returns and tabs with appropriate html for display on page
		var displayText = template.replace(/\n/g, "<br />").replace(/\t/g, "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp");

		var metaModal = $('#meta-modal');
		metaModal.find('.modal-title').text(skillName + ' Meta File');
		metaModal.find('.modal-body').html(displayText);
		metaModal.modal('show');
	}
	else {
		var a = window.document.createElement('a');
		a.href = window.URL.createObjectURL(new Blob([template], { type: 'text/txt' }));
		a.download = skillName + '.json';
		document.body.appendChild(a);
		a.click();
		document.body.removeChild(a);
	}
});

function getGuid(value) {
	var matchArray = value.match(/(\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1}/g);
	return matchArray === null || matchArray.length == 0 ? "" : matchArray[0];
}

// MANAGE
$("#cancel-all-skills").submit(function (e) {

	e.preventDefault();

	if (!ip) {
		showToastMessage("Please enter a valid IP or name.");
		return;
	}

	showToastMessage("Attempting to Cancel All Running Skills...");
	var fetchClient = new FetchClient(ip, 10000);
	fetchClient.PostCommand("skills/cancel", null, function (data) {

		console.log("Skill command response: " + JSON.stringify(data));

		$("#installed-skills").find(".stop").each(function () {
			$(this).removeClass("stop");
			$(this).addClass("start");
			$(this).prop("textContent", "Start");
		});
	});

	fetchClient.PostCommand("drive/stop", null, function (data) {

		console.log("Stop response: " + JSON.stringify(data));
	});
});

function deleteSkillFromRobot() {

	var skillRow = this.parentNode;
	var skillName = skillRow.id;
	var skillId = skillIds[skillName];
	var payload = { "Skill": skillId };

	fetchClient.DeleteCommand("skills?Skill=" + skillId, function (data) {

		console.log("Delete skill response: " + JSON.stringify(data));

		if (data.status == "Success") {

			showToastMessage("Skill " + skillName + " successfully deleted.");
			skillRow.remove();
			delete skillIds[skillName];
		}
		else {
			showToastMessage("Failed to delete skill " + skillName + ".");
		}
	});
}

function startStopSkill() {

	if (!ip) {
		showToastMessage("Please enter a valid IP address.");
		return;
	}

	currentSkillName = this.parentNode.parentNode.id;
	var className = this.className;
	
	if (className.indexOf("stop") < 0) {
		startSkill.call(this);
	}

	else {
		stopSkill.call(this);
	}
}

function startSkill(parameters = null) {

	var payload = parameters ? parameters : {};
	var skillId = skillIds[currentSkillName];
	var skillElement = document.getElementById(currentSkillName);
	var skillStartButton = skillElement.querySelector(".start");

	if (!skillId) {
		showToastMessage("Unable to run skill. Unique id for " + currentSkillName + "not found.");
		return;
	}
	 
	payload["Skill"] = skillId; 

	fetchClient.PostCommand("skills/start", JSON.stringify(payload), function (data) {

		console.log("Skill command response: ...", data);

		if (data.status == "Success") {
			toggleStartStopButton(skillStartButton);
			runningSkills[currentSkillName] = skillId;
			showToastMessage("Running Skill '" + currentSkillName + "'.");
		}
	});
}

function getRunningSkillsCallback(data) {

	var array = data.result;
	Object.keys(runningSkills).forEach(key => {
		if (!array.includes(key)) {

			var skillElement = document.getElementById(key);
			var skillStartButton = skillElement.querySelector(".stop");
			toggleStartStopButton(skillStartButton);
			delete runningSkills[currentSkillName];
		}
	})
}

function stopSkill() {

	var skillId = skillIds[currentSkillName];
	var skillElement = document.getElementById(currentSkillName);
	var skillStartButton = skillElement.querySelector(".stop");

	if (!skillId) {
		showToastMessage("Unable to stop skill. Unique id for " + currentSkillName + "not found.");
		return;
	}

	var payload = { "Skill": skillId }

	showToastMessage("Stopping Skill '" + currentSkillName + "'...");

	fetchClient.PostCommand("drive/stop", null, function (data) {

		console.log("Stop motors response: " + JSON.stringify(data));
	});

	fetchClient.PostCommand("skills/cancel", JSON.stringify(payload), function (data) {

		console.log("Skill command response: " + JSON.stringify(data));
		if (data.status == "Success") {

			toggleStartStopButton(skillStartButton);
			delete runningSkills[currentSkillName];
		}
	});
}

function showAdvancedSettings() {

	var additionalFields = document.getElementsByClassName("additional");

	while (additionalFields[0]) {
		additionalFields[0].parentNode.removeChild(additionalFields[0]);
	}

	var advancedModal = $('#advanced-modal');
	var startButton = document.querySelector("#advanced-start");

	if (runningSkills[currentSkillName]) {
		startButton.className = "btn btn-secondary stop";
		startButton.textContent = "Stop";
	} else {
		startButton.className = "btn btn-secondary start";
		startButton.textContent = "Start";
	}

	if (!ip) {
		advancedModal.find('.modal-title').text("Skill Name");
	}
	else {
		currentSkillName = this.parentNode.id;
		advancedModal.find('.modal-title').text(currentSkillName);
	}

	advancedModal.modal('show');
}

function addKeyValueRow(e) {

	e = e || window.event;
	var target = e.target || e.srcElement;
	
	var keyInput = document.createElement("input");
	keyInput.type = "text";
	keyInput.className = "keyvalue skillkey additional";
	keyInput.placeholder = "Key";

	var valueInput = document.createElement("input");
	valueInput.type = "text";
	valueInput.className = "keyvalue skillvalue additional";
	valueInput.placeholder = "Value";

	var groupDiv = document.createElement("div");
	groupDiv.className = "keyvalue-group";
	groupDiv.append(keyInput);
	groupDiv.append(valueInput);

	var column = $(target).prev();
	column.append(groupDiv);
}

$("#advanced-start").on("click", (function () {

	var params = {};
	var skillElement = document.getElementById(currentSkillName);
	var skillStartButton = this;

	if (skillStartButton.className.indexOf("stop") > -1) {

		stopSkill.call(skillStartButton);

	} else {

		$("#skill-parameters-input").children().each(function () {

			var key = $(this).find(".skillkey").val();
			var value = $(this).find(".skillvalue").val();

			params[key] = value;

			var key = $(this).find(".skillkey").val("");
			var value = $(this).find(".skillvalue").val("");
		});

		startSkill.call(skillStartButton, params);
	}
}));

$("#submit-event").on("click", (function (e) {

	e.preventDefault();

	var eventName = $("#event-name").val();
	var params = {};
	var skillId = skillIds[currentSkillName];

	if (!ip) {
		showToastMessage("Please enter a valid IP or name.");
		return;
	}

	if (!skillId) {
		showToastMessage("Unable to run skill. Unique id for " + currentSkillName + "not found.");
		return;
	}

	if (!eventName) {
		showToastMessage("Please enter the name of the event to send.");
		return;
	}

	$("#skill-event-input").children().each(function () {

		var key = $(this).find(".skillkey").val();
		var value = $(this).find(".skillvalue").val();

		params[key] = value;

		var key = $(this).find(".skillkey").val("");
		var value = $(this).find(".skillvalue").val("");
	});

	var payload = {
		"Skill": skillId,
		"EventName": eventName,
		"Payload": params
	};

	showToastMessage("Sending event '" + eventName + "' to '" + currentSkillName + "'.");

	fetchClient.PostCommand("skills/event", JSON.stringify(payload), function (data) {
		console.log("User event response for event " + eventName + " :" + JSON.stringify(data));
	});
}));

// UTILITIES
function uuidv4() {
	return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
		var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
		return v.toString(16);
	});
}

async function showToastMessage(message, timeInMs = 4000) {

	if (message) {

		console.log(message);

		var element = document.getElementById("toast");
		var snackbar = $('#toast').html(message);
		element.className = "show";

		await sleep(timeInMs);

		element.className = element.className.replace("show", "");
	}
}

function sleep(ms) {
	return new Promise(resolve => setTimeout(resolve, ms));
}

function toggleStartStopButton(button) {

	if (button.className.indexOf("stop") < 0) {
		$(button).removeClass("start");
		$(button).addClass("stop");
		$(button).prop("textContent", "Stop");
	} else {
		$(button).removeClass("stop");
		$(button).addClass("start");
		$(button).prop("textContent", "Start");
	}
}
