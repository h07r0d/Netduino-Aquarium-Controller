var thingspeakInit = function() {
	var pluginEnabled = (config.config.output.Thingspeak.enabled === 'true');
	$("#API").val(config.config.output.Thingspeak.writeapi);
	$("#API").change(function() { config.config.output.Thingspeak.writeapi = $(this).val(); });
	var thingspeakEnable = $('#PThE');
	thingspeakEnable.attr('checked', pluginEnabled);
	thingspeakEnable.click(function() {
		config.config.output.Thingspeak.enabled = this.checked ? 'true' : 'false';
		$(this).button("option","label", this.checked ? "Enabled" : "Disabled"); 
	});
	thingspeakEnable.button({ label: (pluginEnabled ? "Enabled" : "Disabled") }).button('refresh');
};