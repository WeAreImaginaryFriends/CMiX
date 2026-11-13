/* The sandbox of the UI kit: proposals that wait for a decision of the owner.
   A proposal shows one thing as it is now (Before) and as it could be (After), side by side.
   It changes nothing in CMiX and nothing in cmix.css: the After look is in sandbox.css, under the class of the proposal.
   When the owner decides, remove the proposal from this file and its block from sandbox.css.
   The number of a proposal (S1, S2 ...) is only for the discussion. It is not a fixed number of the catalogue.
   Do not use the number of an earlier proposal again. The last number used is S1.

   id: the number. title: what it is about. question: the decision to make, as one question.
   why: the problem of today. effect: what changes in CMiX when the answer is yes.
   before, after: one line under each side. facts: measured values, as [label, before, after].
   sample: the markup of the controls, the same for the two sides. The After side has the class "sb-" + id around it. */
window.CMIX_SANDBOX = [];
