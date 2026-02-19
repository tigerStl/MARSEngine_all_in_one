package com.mars.javaui.record.eventoperation;

import java.awt.Component;
import java.io.Writer;
import java.util.Set;
import java.util.concurrent.atomic.AtomicReference;
import javax.swing.JComboBox;
import javax.swing.JTable;
import javax.swing.Timer;
import javax.swing.event.TreeExpansionListener;
import org.java_websocket.WebSocket;

/** Holds refs and constants for recording; passed to Mouse/Keyboard handlers. */
public final class RecordingContext {

    public final AtomicReference<? extends Writer> writerRef;
    public final AtomicReference<WebSocket> clientConn;
    public final java.io.File outputDir;
    public final boolean[] recording;

    // Mouse click state
    public final long PRESS_RELEASE_MIN_MS;
    public final long PRESS_RELEASE_MAX_MS;
    public final long DBLCLICK_MS;
    public final int SCREEN_POS_TOLERANCE;
    public final int CLICK_MERGE_DISTANCE_PX;
    public final int PENDING_CLICK_DELAY_MS;
    public final long[] lastPressedTimeRef;
    public final Component[] lastPressedComponentRef;
    public final int[] lastPressedXRef;
    public final int[] lastPressedYRef;
    public final int[] lastPressedScreenXRef;
    public final int[] lastPressedScreenYRef;
    public final int[] lastPressedButtonRef;
    public final long[] lastReleasedTimeRef;
    public final Component[] lastReleasedComponentRef;
    public final AtomicReference<Timer> pendingClickTimerRef;
    public final Component[] pendingClickComponentRef;
    public final int[] pendingClickButtonRef;
    public final int[] pendingClickXRef;
    public final int[] pendingClickYRef;
    public final long[] pendingClickReleaseTimeRef;

    // Combo/Edit state
    public final JComboBox<?>[] currentComboBoxRef;
    public final String[] currentComboInitialRef;
    public final String[] currentComboSelectedRef;
    public final boolean[] currentComboInteractedRef;
    public final boolean[] currentComboEmittedRef;
    public final Component[] currentEditComponentRef;
    public final String[] currentEditInitialTextRef;
    public final boolean[] currentEditHadKeyRef;

    // Table state
    public final JTable[] currentTableRef;
    public final int[] currentTableRowRef;
    public final int[] currentTableColRef;
    public final String[] currentTableColumnNameRef;
    public final String[] currentTableInitialValueRef;
    public final boolean[] currentTableHadKeyRef;
    public final boolean[] currentTableValueChangedRef;
    public final boolean[] currentTableEmittedRef;
    public final String[][] currentTableConditionColumnsRef;
    public final String[][] currentTableConditionValuesRef;
    public final long[] lastTableInteractionTimeRef;
    public final JTable[] lastTableRightClickRef;
    public final int[] lastTableRightClickRowRef;
    public final int[] lastTableRightClickColRef;
    public final String[] lastTableRightClickColumnNameRef;
    public final String[] lastTableRightClickCellValueRef;
    public final String[][] lastTableRightClickConditionColumnsRef;
    public final String[][] lastTableRightClickConditionValuesRef;
    public final long[] lastTableRightClickTimeRef;

    // Key state
    public final long[] lastKeyDedupWhenRef;
    public final int[] lastKeyDedupIdRef;
    public final int[] lastKeyDedupCodeRef;
    public final int[] lastKeyDedupModifiersRef;
    public final Object[] lastKeyDedupSourceRef;
    public final Set<Integer> pressedKeyCodes;
    public final StringBuilder typedBuffer;
    public final long[] lastTypedTimeRef;
    public final long KEY_DEDUP_MS;
    public final Component[] lastFillEditComponentRef;
    public final long[] lastFillEditTimeRef;
    public final long FILLEDIT_DEDUPE_MS;

    public RecordingContext(
            AtomicReference<? extends Writer> writerRef,
            AtomicReference<WebSocket> clientConn,
            java.io.File outputDir,
            boolean[] recording,
            long PRESS_RELEASE_MIN_MS,
            long PRESS_RELEASE_MAX_MS,
            long DBLCLICK_MS,
            int SCREEN_POS_TOLERANCE,
            int CLICK_MERGE_DISTANCE_PX,
            int PENDING_CLICK_DELAY_MS,
            long[] lastPressedTimeRef,
            Component[] lastPressedComponentRef,
            int[] lastPressedXRef,
            int[] lastPressedYRef,
            int[] lastPressedScreenXRef,
            int[] lastPressedScreenYRef,
            int[] lastPressedButtonRef,
            long[] lastReleasedTimeRef,
            Component[] lastReleasedComponentRef,
            AtomicReference<Timer> pendingClickTimerRef,
            Component[] pendingClickComponentRef,
            int[] pendingClickButtonRef,
            int[] pendingClickXRef,
            int[] pendingClickYRef,
            long[] pendingClickReleaseTimeRef,
            JComboBox<?>[] currentComboBoxRef,
            String[] currentComboInitialRef,
            String[] currentComboSelectedRef,
            boolean[] currentComboInteractedRef,
            boolean[] currentComboEmittedRef,
            Component[] currentEditComponentRef,
            String[] currentEditInitialTextRef,
            boolean[] currentEditHadKeyRef,
            JTable[] currentTableRef,
            int[] currentTableRowRef,
            int[] currentTableColRef,
            String[] currentTableColumnNameRef,
            String[] currentTableInitialValueRef,
            boolean[] currentTableHadKeyRef,
            boolean[] currentTableValueChangedRef,
            boolean[] currentTableEmittedRef,
            String[][] currentTableConditionColumnsRef,
            String[][] currentTableConditionValuesRef,
            long[] lastTableInteractionTimeRef,
            JTable[] lastTableRightClickRef,
            int[] lastTableRightClickRowRef,
            int[] lastTableRightClickColRef,
            String[] lastTableRightClickColumnNameRef,
            String[] lastTableRightClickCellValueRef,
            String[][] lastTableRightClickConditionColumnsRef,
            String[][] lastTableRightClickConditionValuesRef,
            long[] lastTableRightClickTimeRef,
            long[] lastKeyDedupWhenRef,
            int[] lastKeyDedupIdRef,
            int[] lastKeyDedupCodeRef,
            int[] lastKeyDedupModifiersRef,
            Object[] lastKeyDedupSourceRef,
            Set<Integer> pressedKeyCodes,
            StringBuilder typedBuffer,
            long[] lastTypedTimeRef,
            long KEY_DEDUP_MS,
            Component[] lastFillEditComponentRef,
            long[] lastFillEditTimeRef,
            long FILLEDIT_DEDUPE_MS) {
        this.writerRef = writerRef;
        this.clientConn = clientConn;
        this.outputDir = outputDir;
        this.recording = recording;
        this.PRESS_RELEASE_MIN_MS = PRESS_RELEASE_MIN_MS;
        this.PRESS_RELEASE_MAX_MS = PRESS_RELEASE_MAX_MS;
        this.DBLCLICK_MS = DBLCLICK_MS;
        this.SCREEN_POS_TOLERANCE = SCREEN_POS_TOLERANCE;
        this.CLICK_MERGE_DISTANCE_PX = CLICK_MERGE_DISTANCE_PX;
        this.PENDING_CLICK_DELAY_MS = PENDING_CLICK_DELAY_MS;
        this.lastPressedTimeRef = lastPressedTimeRef;
        this.lastPressedComponentRef = lastPressedComponentRef;
        this.lastPressedXRef = lastPressedXRef;
        this.lastPressedYRef = lastPressedYRef;
        this.lastPressedScreenXRef = lastPressedScreenXRef;
        this.lastPressedScreenYRef = lastPressedScreenYRef;
        this.lastPressedButtonRef = lastPressedButtonRef;
        this.lastReleasedTimeRef = lastReleasedTimeRef;
        this.lastReleasedComponentRef = lastReleasedComponentRef;
        this.pendingClickTimerRef = pendingClickTimerRef;
        this.pendingClickComponentRef = pendingClickComponentRef;
        this.pendingClickButtonRef = pendingClickButtonRef;
        this.pendingClickXRef = pendingClickXRef;
        this.pendingClickYRef = pendingClickYRef;
        this.pendingClickReleaseTimeRef = pendingClickReleaseTimeRef;
        this.currentComboBoxRef = currentComboBoxRef;
        this.currentComboInitialRef = currentComboInitialRef;
        this.currentComboSelectedRef = currentComboSelectedRef;
        this.currentComboInteractedRef = currentComboInteractedRef;
        this.currentComboEmittedRef = currentComboEmittedRef;
        this.currentEditComponentRef = currentEditComponentRef;
        this.currentEditInitialTextRef = currentEditInitialTextRef;
        this.currentEditHadKeyRef = currentEditHadKeyRef;
        this.currentTableRef = currentTableRef;
        this.currentTableRowRef = currentTableRowRef;
        this.currentTableColRef = currentTableColRef;
        this.currentTableColumnNameRef = currentTableColumnNameRef;
        this.currentTableInitialValueRef = currentTableInitialValueRef;
        this.currentTableHadKeyRef = currentTableHadKeyRef;
        this.currentTableValueChangedRef = currentTableValueChangedRef;
        this.currentTableEmittedRef = currentTableEmittedRef;
        this.currentTableConditionColumnsRef = currentTableConditionColumnsRef;
        this.currentTableConditionValuesRef = currentTableConditionValuesRef;
        this.lastTableInteractionTimeRef = lastTableInteractionTimeRef;
        this.lastTableRightClickRef = lastTableRightClickRef;
        this.lastTableRightClickRowRef = lastTableRightClickRowRef;
        this.lastTableRightClickColRef = lastTableRightClickColRef;
        this.lastTableRightClickColumnNameRef = lastTableRightClickColumnNameRef;
        this.lastTableRightClickCellValueRef = lastTableRightClickCellValueRef;
        this.lastTableRightClickConditionColumnsRef = lastTableRightClickConditionColumnsRef;
        this.lastTableRightClickConditionValuesRef = lastTableRightClickConditionValuesRef;
        this.lastTableRightClickTimeRef = lastTableRightClickTimeRef;
        this.lastKeyDedupWhenRef = lastKeyDedupWhenRef;
        this.lastKeyDedupIdRef = lastKeyDedupIdRef;
        this.lastKeyDedupCodeRef = lastKeyDedupCodeRef;
        this.lastKeyDedupModifiersRef = lastKeyDedupModifiersRef;
        this.lastKeyDedupSourceRef = lastKeyDedupSourceRef;
        this.pressedKeyCodes = pressedKeyCodes;
        this.typedBuffer = typedBuffer;
        this.lastTypedTimeRef = lastTypedTimeRef;
        this.KEY_DEDUP_MS = KEY_DEDUP_MS;
        this.lastFillEditComponentRef = lastFillEditComponentRef;
        this.lastFillEditTimeRef = lastFillEditTimeRef;
        this.FILLEDIT_DEDUPE_MS = FILLEDIT_DEDUPE_MS;
    }
}
