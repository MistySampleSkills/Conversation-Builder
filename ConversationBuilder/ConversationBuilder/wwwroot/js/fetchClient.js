// 		Copyright 2019 Misty Robotics, Inc.
// 		Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
// 		to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// 		and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 		The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 		THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// 		FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// 		WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

function FetchClient(ip, ajaxTimeout) {
	var ipAddress = ip === null ? "localhost" : ip;
	var _timeout = ajaxTimeout === null ? 30000 : ajaxTimeout;

	this.SetIp = function (ip) {
		ipAddress = ip;
	};
	this.SetTimeout = function (theTimeout) {
		_timeout = theTimeout;
	};

	this.DeleteCommand = function (command, successCallback = null, failCallback = null) {
		var newUri = "http://" + ipAddress + "/api/" + command;
		Promise.race([
			fetch(newUri, {
				method: 'DELETE',
				dataType: "json"
			}),
			new Promise((_, reject) => setTimeout(() => reject(new Error('timeout')), _timeout))
		])
			//.then(response => response.json())
			.then(function (data) {
				if (data === null || data.status === "Error" || data.status === "Failed") {
					if (failCallback) {
						failCallback('', data, data.error);
					} else {
						console.log("Get Response Error:", data.errorMessage);
					}
				}
				else if (successCallback) {
					
					successCallback(data);
				}
			})
			.catch(function (err) {
				if (failCallback) {
					failCallback('', '', "Failed to connect to the specified url");
				} else {
					// There was an error with the call.  Display error messages.
					console.log("Delete Http Error: Failed to connect to url " + newUri);
				}
			});
	};

	this.GetCommand = function (command, successCallback = null, version = null, failCallback = null) {
		var newUri = "http://" + ipAddress + "/api/" + (version ? version + "/" : "") + command;
		Promise.race([
			fetch(newUri, {
				method: 'GET',
				dataType: "json"
			}),
			new Promise((_, reject) => setTimeout(() => reject(new Error('timeout')), _timeout))
		])
			.then(response => {
					var contentType = response.headers.get('content-type');
					if(contentType === "application/json")
					{
						return response.json();
					}
					//else return bodydata
					var reader = response.body.getReader();
					return reader
						.read()
						.then((result) => {
						return result;
						});				
			})
			.then(function (data) {
				if (data === null || data.status === "Error" || data.status === "Failed") {
					if (failCallback) {
						failCallback('', data, data.error);
					} else {
						console.log("Get " + (version ? version : "") + "Response Error:", data.errorMessage);
					}
				}
				else if (successCallback) {
					// no errors and there is a callback function.
					// no errors and there is a callback function.
				//	var contentType = data.headers.get('content-type');

				/*	if(contentType.includes("json"))
					{
						data = data.json();
					}
					else {
						data = data.blob;
					}*/

					successCallback(data);
				}
			})
			.catch(function (err) {
				if (failCallback) {
					failCallback('', '', "Failed to connect to the specified url");
				} else {
					// There was an error with the call.  Display error messages.
					console.log("Get Http Error: Failed to connect to url " + newUri);
				}
			});
	};

	this.PutCommand = function (command, theData = {}, successCallback = null, dataType = "application/json", failCallback = null) {
		var newUri = "http://" + ipAddress + "/api/" + command;
		Promise.race([
			fetch(newUri, {
				method: "PUT",
				headers: {
					"Accept": dataType,
					"Content-Type": dataType,
				},
				body: theData,
				dataType: "json"
			}),
			new Promise((_, reject) => setTimeout(() => reject(new Error('timeout')), _timeout))
		])
			.then(response => response.json())
			.then(function (data) {
				if (data === null || data.status === "Error" || data.status === "Failed") {
					if (failCallback) {
						failCallback('', data, data.error);
					} else {
						console.log("Post Response Error:", data.errorMessage);
					}
				}
				else if (successCallback) {
					successCallback(data);
				}
			})
			.catch(function (err) {
				if (failCallback) {
					failCallback('', '', "Failed to connect to the specified url");
				} else {
					// There was an error with the call.  Display error messages.
					console.log("Put Http Error: Failed to connect to url " + newUri);
				}
			});
	};

	this.PostCommand = function (command, theData = {}, successCallback = null, version = null, dataType = "application/json", failCallback = null) {
		var newUri = "http://" + ipAddress + "/api/" + (version ? version + "/" : "") + command;
		Promise.race([
			fetch(newUri, {
				method: "POST",
				headers: {
					"Accept": dataType,
					"Content-Type": dataType,
				},
				body: theData,
				dataType: "json"
			}),
			new Promise((_, reject) => setTimeout(() => reject(new Error('timeout')), _timeout))
		])
			.then(response => response.json())
			.then(function (data) {
				if (data === null || data.status === "Error" || data.status === "Failed") {
					if (failCallback) {
						failCallback('', data, data.error);
					} else {
						console.log("Post " + (version ? version : "") + "Response Error:", data.errorMessage);
					}
				}
				else if (successCallback) {
					successCallback(data);
				}
			})
			.catch(function (err) {
				if (failCallback) {
					failCallback('', '', "Failed to connect to the specified url");
				} else {
					// There was an error with the call.  Display error messages.
					console.log("Post Http Error: Failed to connect to url " + newUri);
				}
			});
	};
}