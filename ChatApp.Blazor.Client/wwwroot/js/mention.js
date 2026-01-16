// Mention Panel JS utilities
let dotNetHelper = null;
let panelElement = null;
let keydownHandler = null;

export function initializeMentionPanel(dotNetRef, panelRef) {
    dotNetHelper = dotNetRef;
    panelElement = panelRef;

    // Keyboard event handler
    keydownHandler = async (e) => {
        if (['ArrowDown', 'ArrowUp', 'Enter', 'Escape'].includes(e.key)) {
            e.preventDefault();
            e.stopPropagation();

            try {
                await dotNetHelper.invokeMethodAsync('HandleKeyDown', e.key);
            } catch (error) {
                console.error('Mention panel keyboard error:', error);
            }
        }
    };

    document.addEventListener('keydown', keydownHandler);
}

export function disposeMentionPanel() {
    if (keydownHandler) {
        document.removeEventListener('keydown', keydownHandler);
        keydownHandler = null;
    }
    dotNetHelper = null;
    panelElement = null;
}

// Get caret position in textarea
export function getCaretPosition(textareaRef) {
    if (!textareaRef) return -1;
    return textareaRef.selectionStart || 0;
}

// Set caret position in textarea
export function setCaretPosition(textareaRef, position) {
    if (!textareaRef) return;
    textareaRef.setSelectionRange(position, position);
    textareaRef.focus();
}

// Get text from last @ symbol to caret
export function getTextBeforeCaret(textareaRef) {
    if (!textareaRef) {
        return { Text: '', MentionStart: -1 };
    }

    const caretPos = textareaRef.selectionStart || 0;
    const textBeforeCaret = textareaRef.value.substring(0, caretPos);

    // Find last @ symbol
    const lastAtIndex = textBeforeCaret.lastIndexOf('@');

    if (lastAtIndex === -1) {
        return { Text: '', MentionStart: -1 };
    }

    // Check if @ is at the start or preceded by whitespace
    const charBeforeAt = lastAtIndex > 0 ? textBeforeCaret[lastAtIndex - 1] : ' ';
    const isValidMentionStart = lastAtIndex === 0 || /\s/.test(charBeforeAt);

    if (!isValidMentionStart) {
        return { Text: '', MentionStart: -1 };
    }

    // Extract text after @
    const textAfterAt = textBeforeCaret.substring(lastAtIndex + 1);

    // Check if text after @ contains whitespace (invalid mention)
    if (/\s/.test(textAfterAt)) {
        return { Text: '', MentionStart: -1 };
    }

    return {
        Text: textAfterAt,
        MentionStart: lastAtIndex
    };
}

// Replace text with mention
export function insertMention(textareaRef, mentionStart, searchLength, mentionText) {
    if (!textareaRef) return;

    const currentValue = textareaRef.value;

    // Calculate mention end position (@search -> MentionText)
    const mentionEnd = mentionStart + 1 + searchLength; // +1 for @ symbol

    // Build new text: before @ + MentionText + space + after mention
    // @ simvolu SİLİNİR, yalnız ad yazılır
    const before = currentValue.substring(0, mentionStart);
    const after = currentValue.substring(mentionEnd);
    const newValue = before + mentionText + ' ' + after;

    // Update textarea value
    textareaRef.value = newValue;

    // Set caret position after mention + space
    const newCaretPos = mentionStart + mentionText.length + 1; // +1 for space only
    textareaRef.setSelectionRange(newCaretPos, newCaretPos);
    textareaRef.focus();

    // Trigger input event so Blazor can detect change
    textareaRef.dispatchEvent(new Event('input', { bubbles: true }));
}

// Get panel position relative to textarea caret
export function getPanelPosition(textareaRef) {
    if (!textareaRef) return { left: 0, bottom: 0 };

    const rect = textareaRef.getBoundingClientRect();

    // Position panel above textarea
    return {
        left: rect.left,
        bottom: window.innerHeight - rect.top + 8 // 8px gap
    };
}

// Get textarea value
export function getTextareaValue(textareaRef) {
    if (!textareaRef) return '';
    return textareaRef.value;
}

// Scroll to selected mention item
export function scrollToSelectedMentionItem(listRef, selectedIndex) {
    if (!listRef) return;

    const items = listRef.querySelectorAll('.mention-item');
    if (selectedIndex < 0 || selectedIndex >= items.length) return;

    const selectedItem = items[selectedIndex];
    if (!selectedItem) return;

    // Scroll into view with smooth behavior
    selectedItem.scrollIntoView({
        behavior: 'smooth',
        block: 'nearest'
    });
}