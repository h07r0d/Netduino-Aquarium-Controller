var relayInit = function() {
	var pluginEnabled = (config.config.control.Relays.enabled === 'true');
	var relayEnable = $('#PRE');
	relayEnable.attr('checked', pluginEnabled);
	relayEnable.click(function() {
		config.config.control.Relays.enabled = this.checked ? 'true' : 'false';
		$(this).button("option","label", this.checked ? "Enabled" : "Disabled"); 
	});
	relayEnable.button({ label: (pluginEnabled ? "Enabled" : "Disabled") }).button('refresh');	
		
	$('div[id^="rel"]').each(function() {		
		var relayNumber = $(this).attr("id");
		relayNumber = relayNumber.charAt(relayNumber.length-1);		
		var onValue = ToTicks(config.config.control.Relays.relays[relayNumber-1].on);
		var offValue = ToTicks(config.config.control.Relays.relays[relayNumber-1].off);
		var onButton = $('input[id="en'+relayNumber+'"]');	
		$('div[id="ti'+relayNumber+'"]').html(ToTime(onValue)+" - " + ToTime(offValue));
		$(this).slider({
			range: true,
			min: 0,
			max: 1440,					
			step: 15,
			values:[onValue,offValue],
			slide: function(event, ui) {
				SetTime(ui, relayNumber);
			},
			change: function(event, ui) {
				SetTime(ui, relayNumber);
			}
		});
		onButton.attr('checked', isRelayOn(onValue, offValue));
		// AJAX call to Netduino to toggle relay status
		onButton.click(function() { $(this).button("option","label", this.checked ? "On" : "Off"); });
		/*
			var button = this;
			$.ajax({
				url: '192.168.1.42/relay',
				type: 'POST',
				dataType: 'json',
				data: {
					relay: relayNumber,
					status: button.checked,
				},
				success: function(result) {
					
				}
			});
		});
		*/
		onButton.button({ label: (isRelayOn(onValue, offValue) ? "On" : "Off") }).button('refresh');
	});
};

function SetTime(ui, relay) {
	var start_time = ToTime(ui.values[0]);
	var end_time = ToTime(ui.values[1]);
	config.config.control.Relays.relays[relay-1].on = GetHours(ui.values[0])+''+GetMinutes(ui.values[0]);
	config.config.control.Relays.relays[relay-1].off = GetHours(ui.values[1])+''+GetMinutes(ui.values[1]);
	$('div[id="ti'+relay+'"]').html(start_time +" - " + end_time);
}

function ToTime(ticks) {
	var hours = GetHours(ticks);
	var minutes = GetMinutes(ticks);
	return ZeroPad(hours,2)+":"+ZeroPad(minutes,2);
}

function GetHours(ticks) {
	return Math.floor(ticks / 60);
}

function GetMinutes(ticks) {
	var hours = GetHours(ticks);
	return ticks - (hours * 60);
}

function ToTicks(time) {
	var length = time.toString().length;
	var hours = length == 3 ? time.toString().substring(0,1) : time.toString().substring(0,2);
	var minutes = time.toString().substring(length-2);
	return (hours*60)+Number(minutes);
}

function isRelayOn(start, end) {
	var now = new Date();
	var relayStart = new Date(now.getFullYear(), now.getMonth(), now.getDate(), GetHours(start), GetMinutes(start));
	var relayEnd = new Date(now.getFullYear(), now.getMonth(), now.getDate(), GetHours(end), GetMinutes(end));
	return ( (now >= relayStart) && (now < relayEnd) );	
}

function ZeroPad(num, places) {
	var zero = places - num.toString().length+1;
	return Array(+(zero > 0 && zero)).join("0") + num;
}