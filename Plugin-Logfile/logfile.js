var logfileInit = function() {
	var pluginEnabled = (config.config.output.Logfile.enabled === 'true');
	$("#FN").val(config.config.output.Logfile.filename);
	$("#FN").change(function() { config.config.output.Logfile.filename = $(this).val(); });
	var logfileEnable = $('#PLE');
	logfileEnable.attr('checked', pluginEnabled);
	logfileEnable.click(function() {
		config.config.output.Logfile.enabled = this.checked ? 'true' : 'false';
		$(this).button("option","label", this.checked ? "Enabled" : "Disabled"); 
	});
	logfileEnable.button({ label: (pluginEnabled ? "Enabled" : "Disabled") }).button('refresh');
};