var AquariumStatusInit = function () {
	var pluginEnabled = (config.config.output.AquariumStatus.enabled === 'true');
	var asEnable = $('#PAQE');
	asEnable.attr('checked', pluginEnabled);
	asEnable.click(function () {
		$(this).button("option", "label", this.checked ? "Enabled" : "Disabled");
		config.config.input.AquariumStatus.enabled = this.checked ? 'true' : 'false';
	});
	asEnable.button({ label: (pluginEnabled ? "Enabled" : "Disabled") }).button('refresh');
	$('#status-time').text(aq.time);
	$('#status-temperature').text(aq.Temperature);
	$('#status-ph').text(aq.pH);
};