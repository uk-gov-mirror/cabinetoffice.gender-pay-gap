﻿// Modified from https://github.com/DFEAGILEDEVOPS/cscp-website/blob/c0fe7fa88d129cbb3fdeda64d201421cbb94c4d5/Web/Web.UI/Server/Assets/Scripts/Elements/Forms/live-search.js

(function () {
    "use strict";

    window.GOVUK = window.GOVUK || {};

    function LiveSearch(options) {
        this.state = false;
        this.previousState = false;
        this.resultCache = {};

        this.onRefresh = options.onRefresh;
        this.formId = options.formId;
        this.$form = $("#" + options.formId);
        this.$resultsBlock = options.$results.find('#main');
        this.$loadingBlock = options.$results.find('#loading');
        this.action = this.$form.attr('action') + '-js';
        this.$atomAutodiscoveryLink = options.$atomAutodiscoveryLink;

        if (GOVUK.support.history()) {
            //save the initial state
            this.saveState();
            // store the initial state in browser history
            this.saveHistory();
            // bind events
            this.bindEvents();
            this.bindFilterEvents();
        } else {
            this.$form.find('.js-live-search-fallback').show();
        }
    }
    LiveSearch.prototype.bindEvents = function bindEvents() {
        this.$form = $("#" + this.formId);

        this.$form.find('#ClearFilters').click(this.clearFilters.bind(this));

        $(window).on('popstate', this.popState.bind(this));
        this.clearLoadingIndicator();
        if (this.onRefresh) this.onRefresh();
    };

    LiveSearch.prototype.bindFilterEvents = function bindFilterEvents() {
        this.$form = $("#" + this.formId);
        this.$form.on('change', '.options-container input[type=checkbox], .options-container input[type=radio]', this.formChange.bind(this));
        this.clearLoadingIndicator();
        if (this.onRefresh) this.onRefresh();
    };

    LiveSearch.prototype.saveState = function saveState(newState) {
        if (typeof newState === 'undefined') {
            newState = this.$form.serializeArray();
        }
        this.previousState = this.state;
        this.state = newState;
    };

    LiveSearch.prototype.saveHistory = function saveHistory() {
        history.pushState(this.state, null, window.location.pathname + "?" + $.param(this.state));
    };

    LiveSearch.prototype.popState = function popState(event) {
        if (event.originalEvent.state) {
            this.saveState(event.originalEvent.state);
            this.updateResults();
            this.restoreBooleans();
            this.restoreTextInputs();
        }
    };

    LiveSearch.prototype.formChange = function formChange(e) {
        if (this.isNewState()) {
            this.saveState();
            var pageUpdated = this.updateResults();
            pageUpdated.done(this.saveHistory.bind(this));
        }

        if (e && e.preventDefault) {
            e.preventDefault();
        }
    };

    LiveSearch.prototype.cache = function cache(slug, data) {
        if (typeof data === 'undefined') {
            return this.resultCache[slug];
        } else {
            this.resultCache[slug] = data;
        }
    };

    LiveSearch.prototype.isNewState = function isNewState() {
        return $.param(this.state) !== this.$form.serialize();
    };

    LiveSearch.prototype.updateResults = function updateResults() {
        var searchState = $.param(this.state);
        //var cachedResultData = this.cache(searchState);
        var liveSearch = this;
        //if (typeof cachedResultData === 'undefined') {
            this.showLoadingIndicator();
            return $.ajax({
                url: this.action,
                data: this.state,
                searchState: searchState
            }).done(function (response) {
                liveSearch.cache($.param(liveSearch.state), response);
                liveSearch.displayResults(response, this.searchState);
                liveSearch.clearLoadingIndicator();
            }).error(function () {
                window.location = '/error/1146';
            });
        //} else {
            //this.displayResults(cachedResultData, searchState);
            //var out = new $.Deferred()
            //return out.resolve();
        //}
    };

    LiveSearch.prototype.showLoadingIndicator = function showLoadingIndicator() {
        this.$loadingBlock.text('Loading...');
        this.$loadingBlock.show();
    };

    LiveSearch.prototype.clearLoadingIndicator = function showLoadingIndicator() {
        this.$loadingBlock.html('&nbsp;');
        this.$loadingBlock.hide();
    };

    LiveSearch.prototype.displayResults = function displayResults(results, action) {
        // As search is asynchronous, check that the action associated with these results is
        // still the latest to stop results being overwritten by stale data
        if (action == $.param(this.state)) {
            this.clearLoadingIndicator();
            this.$resultsBlock.html(results);
            //this.$resultsBlock.mustache('finders/_results', results);
            //this.$loadingBlock.mustache('finders/_result_count', results);
            //this.$atomAutodiscoveryLink.attr('href', results.atom_url);
        }
    };

    LiveSearch.prototype.restoreBooleans = function restoreBooleans() {
        var that = this;
        // update the radio buttons
        this.$form.find('.options-container input[type=checkbox], .options-container input[type=radio]').each(function (i, el) {
            var $el = $(el);
            var checked = that.isBooleanSelected($el.attr('name'), $el.attr('value'));
            if ($el.prop('checked') !== checked) $el.click();
        });
    };

    LiveSearch.prototype.isBooleanSelected = function isBooleanSelected(name, value) {
        var i, _i;
        for (i = 0, _i = this.state.length; i < _i; i++) {
            if (this.state[i].name === name && this.state[i].value === value) {
                return true;
            }
        }
        return false;
    };

    LiveSearch.prototype.restoreTextInputs = function restoreTextInputs() {
        var that = this;
        this.$form.find('input[type=text]').each(function (i, el) {
            var $el = $(el);
            $el.val(that.getTextInputValue($el.attr('name')));
        });
    };

    LiveSearch.prototype.getTextInputValue = function getTextInputValue(name) {
        var i, _i;
        for (i = 0, _i = this.state.length; i < _i; i++) {
            if (this.state[i].name === name) {
                return this.state[i].value
            }
        }
        return '';
    };

    LiveSearch.prototype.clearFilters = function (e) {
        this.clearBooleans();
        this.saveState();

        var self = this;
        this.updateResults().done(function () {
            self.saveHistory();
            self.collapseFilters();
        });

        if (e && e.preventDefault) e.preventDefault();
        return false;
    };

    LiveSearch.prototype.clearBooleans = function clearBooleans() {
        this.$form.unbind('change');
        this.$form.find('.options-container input[type=checkbox], .options-container input[type=radio]').each(function (i, el) {
            var $el = $(el);
            if ($el.prop('checked')) $el.click();
        });
        this.bindFilterEvents();
    };

    LiveSearch.prototype.collapseFilters = function collapseFilters() {
        $(".govuk-option-select.js-collapsible .js-container-head[aria-expanded=true]").click();
    };

    GOVUK.LiveSearch = LiveSearch;
}());
