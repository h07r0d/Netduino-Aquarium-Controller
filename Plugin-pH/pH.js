var pHInit = function () {
	var pluginEnabled = (config.config.input.pH.enabled === 'true');
	var phEnable = $('#PpHE');
	phEnable.attr('checked', pluginEnabled);
	phEnable.click(function () {
		$(this).button("option", "label", this.checked ? "Enabled" : "Disabled");
		config.config.input.pH.enabled = this.checked ? 'true' : 'false';
	});
	phEnable.button({ label: (pluginEnabled ? "Enabled" : "Disabled") }).button('refresh');
	$("#phH").val(config.config.input.pH.interval[0]);
	$("#phM").val(config.config.input.pH.interval[1]);
	$("#phS").val(config.config.input.pH.interval[2]);
};