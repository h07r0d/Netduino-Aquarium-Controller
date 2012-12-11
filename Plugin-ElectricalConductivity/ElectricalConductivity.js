var ElectricalConductivityInit = function () {
    var pluginEnabled = (config.config.input.ElectricalConductivity.enabled === 'true');
    var ElectricalConductivityEnable = $('#PElectricalConductivityE');
	ElectricalConductivityEnable.attr('checked', pluginEnabled);
	ElectricalConductivityEnable.click(function () {
		$(this).button("option", "label", this.checked ? "Enabled" : "Disabled");
		config.config.input.ElectricalConductivity.enabled = this.checked ? 'true' : 'false';
	});
	ElectricalConductivityEnable.button({ label: (pluginEnabled ? "Enabled" : "Disabled") }).button('refresh');
	$("#ElectricalConductivityH").val(config.config.input.ElectricalConductivity.interval[0]);
	$("#ElectricalConductivityM").val(config.config.input.ElectricalConductivity.interval[1]);
	$("#ElectricalConductivityS").val(config.config.input.ElectricalConductivity.interval[2]);
};