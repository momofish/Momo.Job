/*!
 *  This script is demonstrating the new CasperJS Navigation Step Flow Control
 *  by adding new functions label() and goto().
 *
 *  As a sample, this 'do_while.js' is to display 1 to 10 by loop like  do{...}while(...)
 *  with Dump Navigation Steps by dumpSteps() function.
 */

//================================================================================

var casper = require('casper').create({
    verbose: false,          // true or false
    logLevel: 'info',      // 'debug' 'info' 'warning' 'error'
});

//================================================================================
//================================================================================
// Extending Casper functions for realizing label() and goto()
// 
// Functions:
//   checkStep()   Revised original checkStep()
//   then()        Revised original then()
//   label()       New function for making empty new navigation step and affixing the new label on it.
//   goto()        New function for jumping to the labeled navigation step that is affixed by label()
//   dumpSteps()   New function for Dump Navigation Steps. This is very helpful as a flow control debugging tool.
// 

var utils = require('utils');
var f = utils.format;

/**
 * Revised checkStep() function for realizing label() and goto()
 * Every revised points are commented.
 *
 * @param  Casper    self        A self reference
 * @param  function  onComplete  An options callback to apply on completion
 */
casper.checkStep = function checkStep(self, onComplete) {
    if (self.pendingWait || self.loadInProgress) {
        return;
    }
    self.current = self.step;                 // Added:  New Property.  self.current is current execution step pointer
    var step = self.steps[self.step++];
    if (utils.isFunction(step)) {
        self.runStep(step);
        step.executed = true;                 // Added:  This navigation step is executed already or not.
    } else {
        self.result.time = new Date().getTime() - self.startTime;
        self.log(f("Done %s steps in %dms", self.steps.length, self.result.time), "info");
        clearInterval(self.checker);
        self.emit('run.complete');
        if (utils.isFunction(onComplete)) {
            try {
                onComplete.call(self, self);
            } catch (err) {
                self.log("Could not complete final step: " + err, "error");
            }
        } else {
            // default behavior is to exit
            self.exit();
        }
    }
};


/**
 * Revised then() function for realizing label() and goto()
 * Every revised points are commented.
 *
 * @param  function  step  A function to be called as a step
 * @return Casper
 */
casper.then = function then(step) {
    if (!this.started) {
        throw new CasperError("Casper not started; please use Casper#start");
    }
    if (!utils.isFunction(step)) {
        throw new CasperError("You can only define a step as a function");
    }
    // check if casper is running
    if (this.checker === null) {
        // append step to the end of the queue
        step.level = 0;
        this.steps.push(step);
        step.executed = false;                 // Added:  New Property. This navigation step is executed already or not.
        this.emit('step.added', step);         // Moved:  from bottom
    } else {

        if (!this.steps[this.current].executed) {  // Added:  Add step to this.steps only in the case of not being executed yet.
            // insert substep a level deeper
            try {
                //          step.level = this.steps[this.step - 1].level + 1;   <=== Original
                step.level = this.steps[this.current].level + 1;   // Changed:  (this.step-1) is not always current navigation step
            } catch (e) {
                step.level = 0;
            }
            var insertIndex = this.step;
            while (this.steps[insertIndex] && step.level === this.steps[insertIndex].level) {
                insertIndex++;
            }
            this.steps.splice(insertIndex, 0, step);
            step.executed = false;                    // Added:  New Property. This navigation step is executed already or not.
            this.emit('step.added', step);            // Moved:  from bottom
        }                                           // Added:  End of if() that is added.

    }
    //    this.emit('step.added', step);   // Move above. Because then() is not always adding step. only first execution time.
    return this;
};


/**
 * Adds a new navigation step by 'then()'  with naming label
 *
 * @param    String    labelname    Label name for naming execution step
 */
casper.label = function label(labelname) {
    var step = new Function('"empty function for label: ' + labelname + ' "');   // make empty step
    step.label = labelname;                                 // Adds new property 'label' to the step for label naming
    this.then(step);                                        // Adds new step by then()
};

/**
 * Goto labeled navigation step
 *
 * @param    String    labelname    Label name for jumping navigation step
 */
casper.goto = function goto(labelname) {
    for (var i = 0; i < this.steps.length; i++) {         // Search for label in steps array
        if (this.steps[i].label == labelname) {      // found?
            this.step = i;                              // new step pointer is set
        }
    }
};
// End of Extending Casper functions for realizing label() and goto()
//================================================================================
//================================================================================



//================================================================================
//================================================================================
// Extending Casper functions for dumpSteps()

/**
 * Dump Navigation Steps for debugging
 * When you call this function, you cat get current all information about CasperJS Navigation Steps
 * This is compatible with label() and goto() functions already.
 *
 * @param   Boolen   showSource    showing the source code in the navigation step?
 *
 * All step No. display is (steps array index + 1),  in order to accord with logging [info] messages.
 *
 */
casper.dumpSteps = function dumpSteps(showSource) {
    this.echo("=========================== Dump Navigation Steps ==============================", "RED_BAR");
    if (this.current) { this.echo("Current step No. = " + (this.current + 1), "INFO"); }
    this.echo("Next    step No. = " + (this.step + 1), "INFO");
    this.echo("steps.length = " + this.steps.length, "INFO");
    this.echo("================================================================================", "WARNING");

    for (var i = 0; i < this.steps.length; i++) {
        var step = this.steps[i];
        var msg = "Step: " + (i + 1) + "/" + this.steps.length + "     level: " + step.level
        if (step.executed) { msg = msg + "     executed: " + step.executed }
        var color = "PARAMETER";
        if (step.label) { color = "INFO"; msg = msg + "     label: " + step.label }

        if (i == this.current) {
            this.echo(msg + "     <====== Current Navigation Step.", "COMMENT");
        } else {
            this.echo(msg, color);
        }
        if (showSource) {
            this.echo("--------------------------------------------------------------------------------");
            this.echo(this.steps[i]);
            this.echo("================================================================================", "WARNING");
        }
    }
};

// End of Extending Casper functions for dumpSteps()
//================================================================================
//================================================================================


// utils
Date.prototype.toIdString = function () {
    function pad(n) { return n < 10 ? '0' + n : n; }
    return this.getFullYear() + '-' +
        pad(this.getMonth() + 1) + '-' +
        pad(this.getDate()) + 'T' +
        pad(this.getHours()) +
        pad(this.getMinutes()) +
        pad(this.getSeconds());
};

Object.prototype.merge = function (obj) {
    for (var key in obj) {
        this[key] = this[key] || obj[key]
    }
}

// my debug
var debugFlag = casper.cli.raw.get('debug');
var batch = new Date().toIdString();
var captureIndex = 0;
function myDebug(tag) {
    tag = tag || "";
    if ((debugFlag & 1) == 1) {
        casper.debugHTML();
    }
    if ((debugFlag & 2) == 2) {
        casper.capture("capture/" + batch + "_" + (++captureIndex) + "_" + tag + ".png");
    }
}

// casper option
casper.options.verbose = !!debugFlag;
casper.options.pageSettings.userAgent = 'Mozilla/5.0 (iPhone; CPU iPhone OS 6_0 like Mac OS X) AppleWebKit/536.26 (KHTML, like Gecko) Version/6.0 Mobile/10A5376e Safari/8536.25';
casper.options.pageSettings.loadImages = false;
phantom.outputEncoding = "gbk";
casper.options.waitTimeout = 90000;
casper.options.clientScripts = [
    'includes/jquery-1.9.1.min.js'
];

// var
var config = JSON.parse(casper.cli.raw.get('config') || null) ||
    {
        startPage: "http://www.gzsums.net/fuyi.aspx?tid=808",
        itemSelector: ".dotbox ul > li",
        titlePattern: "招聘",
        contentPattern: "呼吸内科",
        properties: {
            url: { source: "item", selector: "a", parser: "[0].href" },
            title: { source: "item", selector: "a" },
            pubTime: { source: "item", parser: "function(){ return this.childNodes[2].data; }" },
            content: { source: "detail", selector: "#right_box > div > div:nth-child(4) > div:nth-child(3)", parser: "function(){return $(this).html();}" },
            contentText: { source: "detail", selector: "#right_box > div > div:nth-child(4) > div:nth-child(3)" }
        },
        latestUrl: null
    }
var items;

// capster flow
casper.start();

// 打开起始页面
casper.open(config.startPage);

// 获取列表
casper.then(function list() {
    myDebug("list");
    items = this.evaluate(function (config) {
        var items = [];
        $(config.itemSelector).each(function () {
            var item = {};
            var $item = $(this);
            var properties = config.properties;
            for (var name in properties) {
                var property = properties[name]
                if (property.source != 'item') continue;

                var $property = $item;
                if (property.selector)
                    $property = $item.find(property.selector);
                var parser = property.parser || '.text()'
                if (parser.substr(0, 8) == 'function')
                    parser = eval('(' + parser + ')');
                var value = typeof parser == 'function' ? parser.call($property[0]) : eval('$property' + parser);
                item[name] = value.trim()
            }
            // 到达上次搜索位置则停止
            if (item.url == config.latestUrl)
                return false;
            // 标题不符合搜索关键字则丢弃
            if (config.titlePattern && !new RegExp(config.titlePattern).test(item.title))
                return;
            items.push(item)
        });

        return items;
    }, config);
    //console.log(JSON.stringify(items));
});

var index = 0;
casper.label("LIST_START");

casper.then(function () {
    if (index >= items.length)
        return;

    var url = items[index].url;
    this.open(url);
});

casper.then(function detail() {
    if (index >= items.length)
        return;

    myDebug("detail");
    var item = items[index];
    var detail = this.evaluate(function (config) {
        var detail = {};

        var properties = config.properties;
        for (var name in properties) {
            var property = properties[name]
            if (property.source != 'detail') continue;

            var $detail = $(document);
            var $property = $detail;
            if (property.selector)
                $property = $detail.find(property.selector);
            var parser = property.parser || '.text()'
            if (parser.substr(0, 8) == 'function')
                parser = eval('(' + parser + ')');
            var value = typeof parser == 'function' ? parser.call($property[0]) : eval('$property' + parser);
            detail[name] = value.trim()
        }
        return detail;
    }, config);
    item.merge(detail);
    // 内容不符合搜索关键字则丢弃
    if (config.contentPattern && !new RegExp(config.contentPattern).test(item.contentText))
        return;
    console.log(JSON.stringify(item));
});

// 下一行
casper.then(function gotoListStart() {
    index++;
    if (index < items.length) {
        this.goto("LIST_START");
    }
});

casper.run();