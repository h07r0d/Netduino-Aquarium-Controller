var temperatureInit = function () {
	var pluginEnabled = (config.config.input.Temperature.enabled === 'true');
	var temperatureEnable = $('#PTE');
	temperatureEnable.attr('checked', pluginEnabled);
	temperatureEnable.click(function () {
		$(this).button("option", "label", this.checked ? "Enabled" : "Disabled");
		config.config.input.Temperature.enabled = this.checked ? 'true' : 'false';
	});
	temperatureEnable.button({ label: (pluginEnabled ? "Enabled" : "Disabled") }).button('refresh');
	$("#tiH").val(config.config.input.Temperature.interval[0]);
	$("#tiM").val(config.config.input.Temperature.interval[1]);
	$("#tiS").val(config.config.input.Temperature.interval[2]);
};