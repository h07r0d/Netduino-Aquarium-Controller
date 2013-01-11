var CO2Init = function () {
	var pluginEnabled = (config.config.input.CO2.enabled === 'true');
	var co2Enable = $('#CO2');
	co2Enable.attr('checked', pluginEnabled);
	co2Enable.click(function () {
		$(this).button("option", "label", this.checked ? "Enabled" : "Disabled");
		config.config.input.CO2.enabled = this.checked ? 'true' : 'false';
	});
	co2Enable.button({ label: (pluginEnabled ? "Enabled" : "Disabled") }).button('refresh');
	$("#co2H").val(config.config.input.CO2.interval[0]);
	$("#co2M").val(config.config.input.CO2.interval[1]);
	$("#co2S").val(config.config.input.CO2.interval[2]);
};