var RelaysInit = function () {
    var pluginEnabled = (config.config.control.Relays.enabled == 'true');
    var relayEnable = $('#PRE');
    relayEnable.attr('checked', pluginEnabled);
    relayEnable.click(function () {
        config.config.control.Relays.enabled = this.checked ? 'true' : 'false';
        $(this).button("option", "label", this.checked ? "Enabled" : "Disabled");
    });
    relayEnable.button({ label: (pluginEnabled ? "Enabled" : "Disabled") }).button('refresh');

    $('div[class^="conditionalFields"]').each(function () {
        var relayNumber = parseInt($(this).attr("class").replace("conditionalFields", ""));
        var Enable = (config.config.control.Relays.relays[relayNumber - 1].Enable === 'true');
        var RelayType = config.config.control.Relays.relays[relayNumber - 1].type;
        var Name = config.config.control.Relays.relays[relayNumber - 1].name;
        var RelayTypeSelectBox = document.getElementById("Relay" + relayNumber + "Type");
        var RelayNameField = $("#Relay" + relayNumber + "Name");

        //Populate RangeMetric Boxes
        var RangeMetric = document.getElementById("Relay" + relayNumber + "RangeMetric");
        for (var prop in aq) {
            if (aq.hasOwnProperty(prop)) {
                //alert("prop: " + prop + " value: " + aq[prop])
                if (!(prop === "time"))
                    RangeMetric.options[RangeMetric.options.length] = new Option(prop, prop);
            }
        }

        //Set what happens when name is changed
        RelayNameField.change(function () {
            config.config.control.Relays.relays[relayNumber - 1].name = RelayNameField.val();
        });

        //Set what happens when RelayType is changed.
        RelayTypeSelectBox.onchange = function () {
            RelayTypeChanged(relayNumber);
        };

        //Set what happens when the enable/disable button is clicked.
        var rEnable = $("#Relay" + relayNumber + "Enable");
        rEnable.attr('checked', Enable);
        rEnable.click(function () {
            Enable = this.checked
            $(this).button("option", "label", this.checked ? "Enabled" : "Disabled");
            EnableClicked(relayNumber, Enable);
        });
        rEnable.button({ label: (Enable ? "Enabled" : "Disabled") }).button('refresh');
        EnableClicked(relayNumber, Enable);

        //Populate name field
        RelayNameField.val(Name);

        //Populate the RangeType Box.
        for (var i = 0; i < RelayTypeSelectBox.options.length; i++) {
            if (RelayTypeSelectBox.options[i].value === RelayType) {
                RelayTypeSelectBox.selectedIndex = i;
                RelayTypeSelectBox.onchange();
                break;
            }
        }
    });
};


function EnableClicked(relayNumber, Enable) {
    if (Enable) {
        $('.conditionalFields' + relayNumber).show();
    } else {
        $('.conditionalFields' + relayNumber).hide();
    }
    config.config.control.Relays.relays[relayNumber - 1].Enable = Enable ? 'true' : 'false';
}

function RelayTypeChanged(relayNumber) {
    var RelayType = document.getElementById("Relay" + relayNumber + "Type").value;
    config.config.control.Relays.relays[relayNumber - 1].type = RelayType

    var TimerFields = $('.RelayTimerFields' + relayNumber);
    var DailyTimerFields = $('.RelayDailyTimerFields' + relayNumber);
    var RangeFields = $('.RelayRangeFields' + relayNumber);
    var onButton = $('input[id="en' + relayNumber + '"]');
    var TimesLabel = $('div[id="ti' + relayNumber + '"]')
    var TimeSlider = $('div[id="rel' + relayNumber + '"]')
    var onDuration = $('input[id="Relay' + relayNumber + 'DurationOn"]');
    var OffDuration = $('input[id="Relay' + relayNumber + 'DurationOff"]');
    var RangeMetric = $('select[id="Relay' + relayNumber + 'RangeMetric"]');
    var RangeMax = $('input[id="Relay' + relayNumber + 'RangeMax"]');
    var RangeMin = $('input[id="Relay' + relayNumber + 'RangeMin"]');
    var RangePulseTime = $('input[id="Relay' + relayNumber + 'PulseTime"]');
    var RangePulseSpace = $('input[id="Relay' + relayNumber + 'PulseSpace"]');
    var chkInverted = $("#Relay" + relayNumber + "Inverted");

    switch (RelayType) {
        case "DailyTimer":
            DailyTimerFields.show();
            RangeFields.hide();
            TimerFields.hide();

            //assign data to all fields for this relay and this relay type and then show them or add them.
            if (config.config.control.Relays.relays[relayNumber - 1].on == undefined) config.config.control.Relays.relays[relayNumber - 1].on = "00:00";
            if (config.config.control.Relays.relays[relayNumber - 1].off == undefined) config.config.control.Relays.relays[relayNumber - 1].off = "03:00";

            var onValue = ToTicks(config.config.control.Relays.relays[relayNumber - 1].on);
            var offValue = ToTicks(config.config.control.Relays.relays[relayNumber - 1].off);

            TimesLabel.html(ToTime(onValue) + " - " + ToTime(offValue));
            TimeSlider.slider({
                range: true,
                min: 0,
                max: 1440,
                step: 15,
                values: [onValue, offValue],
                slide: function (event, ui) {
                    SetTime(ui, relayNumber);
                },
                change: function (event, ui) {
                    SetTime(ui, relayNumber);
                }
            });
            onButton.attr('checked', isRelayOn(onValue, offValue));
            // AJAX call to Netduino to toggle relay status
            onButton.click(function () {
                $(this).button("option", "label", this.checked ? "On" : "Off");
                var button = this;
                $.ajax({
                    url: '/Relays',
                    type: 'GET',
                    dataType: 'json',
                    data: {
                        relay: relayNumber - 1,	//Relays are 0 indexed on the control system.
                        status: button.checked,
                    },
                    success: function (result) {

                    }
                });
            });
            onButton.button({ label: (isRelayOn(onValue, offValue) ? "On" : "Off") }).button('refresh');
            break;
        case "Timer":
            DailyTimerFields.hide();
            RangeFields.hide();
            TimerFields.show();

            //Set default valuse if none are set.
            if (config.config.control.Relays.relays[relayNumber - 1].DurationOn === undefined)
                config.config.control.Relays.relays[relayNumber - 1].DurationOn = [0, 15, 0];
            if (config.config.control.Relays.relays[relayNumber - 1].DurationOff === undefined)
                config.config.control.Relays.relays[relayNumber - 1].DurationOff = [0, 15, 0];

            //load values
            onDuration.val(config.config.control.Relays.relays[relayNumber - 1].DurationOn);
            OffDuration.val(config.config.control.Relays.relays[relayNumber - 1].DurationOff);

            //Save values on change
            onDuration.change(function () {
                config.config.control.Relays.relays[relayNumber - 1].DurationOn = onDuration.val();
            });

            OffDuration.change(function () {
                config.config.control.Relays.relays[relayNumber - 1].DurationOff = offDuration.val();
            });

            break;
        case "Range":
            DailyTimerFields.hide();
            RangeFields.show();
            TimerFields.hide();
            //Create a button for the inverted checkbox and set it.
            var Inverted = (config.config.control.Relays.relays[relayNumber - 1].Inverted === 'true');
            chkInverted.attr('checked', Inverted);
            chkInverted.click(function () {
                config.config.control.Relays.relays[relayNumber - 1].Inverted = this.checked ? 'true' : 'false';
                $(this).button("option", "label", this.checked ? "Inverted" : "Non-Inverted");
            });
            chkInverted.button({ label: (Inverted ? "Inverted" : "Non-Inverted") }).button('refresh');
            //Set Values for Range
            RangeMetric.val(config.config.control.Relays.relays[relayNumber - 1].RangeMetric);
            RangeMax.val(config.config.control.Relays.relays[relayNumber - 1].max);
            RangeMin.val(config.config.control.Relays.relays[relayNumber - 1].min);
            RangePulseTime.val(config.config.control.Relays.relays[relayNumber - 1].PulseTime);
            RangePulseSpace.val(config.config.control.Relays.relays[relayNumber - 1].PulseSpace);

            //Update Config variable if settings are changed.
            RangeMetric.change(function () {
                config.config.control.Relays.relays[relayNumber - 1].RangeMetric = RangeMetric.val();
            });
            RangeMax.change(function () {
                config.config.control.Relays.relays[relayNumber - 1].max = RangeMax.val();
            });
            RangeMin.change(function () {
                config.config.control.Relays.relays[relayNumber - 1].max = RangeMin.val();
            });
            RangePulseTime.change(function () {
                config.config.control.Relays.relays[relayNumber - 1].PulseTime = RangePulseTime.val();
            });
            RangePulseSpace.change(function () {
                config.config.control.Relays.relays[relayNumber - 1].PulseSpace = RangePulseSpace.val();
            });
            break;
    }
}

function SetTime(ui, relay) {
    var start_time = ToTime(ui.values[0]);
    var end_time = ToTime(ui.values[1]);
    config.config.control.Relays.relays[relay - 1].on = GetHours(ui.values[0]) + '' + GetMinutes(ui.values[0]);
    config.config.control.Relays.relays[relay - 1].off = GetHours(ui.values[1]) + '' + GetMinutes(ui.values[1]);
    $('div[id="ti' + relay + '"]').html(start_time + " - " + end_time);
}

function ToTime(ticks) {
    var hours = GetHours(ticks);
    var minutes = GetMinutes(ticks);
    return ZeroPad(hours, 2) + ":" + ZeroPad(minutes, 2);
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
    var hours = length == 3 ? time.toString().substring(0, 1) : time.toString().substring(0, 2);
    var minutes = time.toString().substring(length - 2);
    return (hours * 60) + Number(minutes);
}

function isRelayOn(start, end) {
    var now = new Date();
    var relayStart = new Date(now.getFullYear(), now.getMonth(), now.getDate(), GetHours(start), GetMinutes(start));
    var relayEnd = new Date(now.getFullYear(), now.getMonth(), now.getDate(), GetHours(end), GetMinutes(end));
    return ((now >= relayStart) && (now < relayEnd));
}

function ZeroPad(num, places) {
    var zero = places - num.toString().length + 1;
    return Array(+(zero > 0 && zero)).join("0") + num;
}