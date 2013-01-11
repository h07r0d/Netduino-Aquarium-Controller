var DHTSensorInit = function () {
    var pluginEnabled = (config.config.input.DHTSensor.enabled === 'true');
    var DHTSensorEnable = $('#PDHTSensorE');
    DHTSensorEnable.attr('checked', pluginEnabled);
    DHTSensorEnable.click(function () {
		$(this).button("option", "label", this.checked ? "Enabled" : "Disabled");
		config.config.input.DHTSensor.enabled = this.checked ? 'true' : 'false';
	});
DHTSensorEnable.button({ label: (pluginEnabled ? "Enabled" : "Disabled") }).button('refresh');
$("#DHTSensoriH").val(config.config.input.DHTSensor.interval[0]);
$("#DHTSensoriM").val(config.config.input.DHTSensor.interval[1]);
$("#DHTSensoriS").val(config.config.input.DHTSensor.interval[2]);
};