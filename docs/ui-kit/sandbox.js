/* The sandbox of the UI kit: proposals that wait for a decision of the owner.
   A proposal shows one thing as it is now (Before) and as it could be (After), side by side.
   It changes nothing in CMiX and nothing in cmix.css: the After look is in sandbox.css, under the class of the proposal.
   When the owner decides, remove the proposal from this file and its block from sandbox.css.
   The number of a proposal (S1, S2 ...) is only for the discussion. It is not a fixed number of the catalogue.

   id: the number. title: what it is about. question: the decision to make, as one question.
   why: the problem of today. effect: what changes in CMiX when the answer is yes.
   before, after: one line under each side. facts: measured values, as [label, before, after].
   sample: the markup of the controls, the same for the two sides. The After side has the class "sb-" + id around it. */
window.CMIX_SANDBOX = [
    {
        id: 'S1',
        title: 'Text on a control that is on or selected',
        question: 'Make the text white on a control that is on or selected, and make its hover fill a little darker?',
        why: 'Today the text keeps the normal text color (Text #E0E0E0) on the accent. Under the pointer the fill becomes AccentPrimary #5680C2, the lightest blue, and the text is the least readable exactly when the pointer is on it.',
        effect: 'One new color (#4B74B6) for "on + hover" in place of AccentPrimary, and the text color ActiveText on these states. It applies to every toggle, radio button, list row, combo row and checked menu row. AccentPrimary stays for lines: the slider, the drop line, the focus line.',
        before: 'On: AccentSecondary #4D6490, text #E0E0E0. On + hover: AccentPrimary #5680C2, text #E0E0E0.',
        after: 'On: #4D6490 (the same), text #FFFFFF. On + hover: #4B74B6, text #FFFFFF.',
        facts: [
            ['Contrast of the text, on', '4.49 : 1', '5.93 : 1'],
            ['Contrast of the text, on + hover', '3.02 : 1', '4.70 : 1'],
            ['The least contrast for small text (WCAG AA)', '4.5 : 1', '4.5 : 1']
        ],
        sample:
            '<div class="sb-grid">' +
            '<span></span><span class="sb-col">On / selected</span><span class="sb-col">+ hover</span>' +
            '<span class="sb-rowname">ToggleButton</span>' +
            '<button class="ToggleButton checked" style="width:128px">Toggle</button>' +
            '<button class="ToggleButton checked pointerover" style="width:128px">Toggle</button>' +
            '<span class="sb-rowname">RadioButton</span>' +
            '<button class="RadioButton checked" style="width:128px">Radio</button>' +
            '<button class="RadioButton checked pointerover" style="width:128px">Radio</button>' +
            '<span class="sb-rowname">ListBoxItemCMiX</span>' +
            '<div class="ListBoxItemCMiX selected" style="width:128px"><span>Layer 1</span></div>' +
            '<div class="ListBoxItemCMiX selected pointerover" style="width:128px"><span>Layer 1</span></div>' +
            '<span class="sb-rowname">ComboBoxItemCMiX</span>' +
            '<div class="ComboBoxItem selected" style="width:128px">Multiply</div>' +
            '<div class="ComboBoxItem selected pointerover" style="width:128px">Multiply</div>' +
            '</div>'
    }
];
