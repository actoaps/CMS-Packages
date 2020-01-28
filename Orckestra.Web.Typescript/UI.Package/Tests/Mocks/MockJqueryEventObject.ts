///<reference path='../../Typings/tsd.d.ts' />
///<reference path='../../Typescript/Mvc/IControllerContext.ts' />

module Orckestra.Composer.Mocks {
    'use strict';

    export var MockJqueryEventObject: JQueryEventObject = {
        altKey: false,
        AT_TARGET: 0,
        bubbles: false,
        BUBBLING_PHASE: 0,
        button: 0,
        cancelable: false,
        cancelBubble: true,
        CAPTURING_PHASE: 0,
        char: {},
        charCode: 0,
        clientX: 0,
        clientY: 0,
        ctrlKey: false,
        currentTarget: document.createElement('div'),
        data: {},
        defaultPrevented: false,
        delegateTarget: document.createElement('div'),
        eventPhase: 0,
        initEvent: () => { return void 0; },
        isDefaultPrevented: () => { return false; },
        isImmediatePropagationStopped: () => { return false; },
        isPropagationStopped: () => { return false; },
        isTrusted: false,
        key: {},
        keyCode: 0,
        metaKey: false,
        namespace: '',
        originalEvent: null,
        offsetX: 0,
        offsetY: 0,
        pageX: 0,
        pageY: 0,
        preventDefault: () => { return void 0; },
        relatedTarget: document.createElement('div'),
        screenX: 0,
        screenY: 0,
        shiftKey: false,
        srcElement: document.createElement('div'),
        stopImmediatePropagation: () => { return void 0; },
        stopPropagation: () => { return void 0; },
        result: {},
        returnValue: false,
        target: document.createElement('div'),
        type: '',
        timeStamp: 0,
        which: 0
    };
}
