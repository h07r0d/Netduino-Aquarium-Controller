var AquariumStatusInit = function () {
    var ThingSpeakChannel = config.config.output.Thingspeak.channel;
    var ThingSpeakKey = config.config.output.Thingspeak.readapi;
	var pluginEnabled = (config.config.output.AquariumStatus.enabled === 'true');
	var asEnable = $('#PAQE');
	asEnable.attr('checked', pluginEnabled);
	asEnable.click(function () {
		$(this).button("option", "label", this.checked ? "Enabled" : "Disabled");
		config.config.input.AquariumStatus.enabled = this.checked ? 'true' : 'false';
	});
	asEnable.button({ label: (pluginEnabled ? "Enabled" : "Disabled") }).button('refresh');
	$('#status-time').text(aq.time);
	$('#status-Temperature').html('<a href="' + "https://www.thingspeak.com/channels/" + ThingSpeakChannel + "/charts/1?key=" + ThingSpeakKey + '">' + aq.Temperature + '</a>');
	$('#status-pH').html('<a href="' + "https://www.thingspeak.com/channels/" + ThingSpeakChannel + "/charts/2?key=" + ThingSpeakKey + '">' + aq.pH + '</a>');
	$('#status-Microsiemens').html('<a href="' + "https://www.thingspeak.com/channels/" + ThingSpeakChannel + "/charts/4?key=" + ThingSpeakKey + '">' + aq.Microsiemens + '</a>');
	$('#status-TDS').html('<a href="' + "https://www.thingspeak.com/channels/" + ThingSpeakChannel + "/charts/5?key=" + ThingSpeakKey + '">' + aq.TDS + '</a>');
	$('#status-Salinity').html('<a href="' + "https://www.thingspeak.com/channels/" + ThingSpeakChannel + "/charts/6?key=" + ThingSpeakKey + '">' + aq.Salinity + '</a>');
	$('#status-CO2').html('<a href="' + "https://www.thingspeak.com/channels/" + ThingSpeakChannel + "/charts/3?key=" + ThingSpeakKey + '">' + aq.CO2 + '</a>');
	$('#status-AirTemperature').html('<a href="' + "https://www.thingspeak.com/channels/" + ThingSpeakChannel + "/charts/7?key=" + ThingSpeakKey + '">' + aq.AirTemperature + '</a>');
	$('#status-Humidity').html('<a href="' + "https://www.thingspeak.com/channels/" + ThingSpeakChannel + "/charts/8?key=" + ThingSpeakKey + '">' + aq.Humidity + '</a>');
};