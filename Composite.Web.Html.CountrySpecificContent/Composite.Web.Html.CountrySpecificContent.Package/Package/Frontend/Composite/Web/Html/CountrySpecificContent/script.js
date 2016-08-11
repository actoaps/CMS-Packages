﻿(function ($) {
	var countryCookiesName = 'Composite.Web.Html.CountrySpecificContent.UserCountry';

	function getActiveCountry() {
		var country = getCookies(countryCookiesName);
		if (country == undefined) {
			$.ajax({
				async: false,
				url: "//freegeoip.net/json/",
				dataType: 'json',
				success: function (json) {
					country = JSON.stringify(json);
					setCookies(countryCookiesName, country);
				}
			});
		}
		return JSON.parse(country);
	}

	function getCookies(cookiesName) {
		if (typeof (Storage) !== "undefined") {
			return sessionStorage.getItem(cookiesName);
		}
		return undefined;
	}

	function setCookies(cookiesName, value) {
		if (typeof (Storage) !== "undefined") {
			sessionStorage.setItem(cookiesName, value);
		}
	}

	$(document).ready(function () {
		var activeCountry = getActiveCountry();
		$(".content-for-country").each(function () {
			var countries = $(this).data("countries").split(',');
			if ($.inArray(activeCountry.country_code, countries) >= 0) {
				var newhtml = $(this).html()
				.replace("{country_name}", activeCountry.country_name)
				.replace("{country_code}", activeCountry.country_code)
				.replace("{city}", activeCountry.city)
				.replace("{region_name}", activeCountry.region_name);
				$(this).html(newhtml).show();
			} else {
				$(this).remove();
			}
		});
	});
})(jQuery)