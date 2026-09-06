package com.northstar.capital.model;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

public final class ValidationResult {
    private final boolean passed;
    private final String message;
    private final List<String> invalidFields;

    private ValidationResult(boolean passed, String message, List<String> invalidFields) {
        this.passed = passed;
        this.message = message;
        this.invalidFields = Collections.unmodifiableList(new ArrayList<>(invalidFields));
    }

    public static ValidationResult passed() {
        return new ValidationResult(true, "Validation: PASSED", List.of());
    }

    public static ValidationResult failed(String reason, String... fields) {
        List<String> invalid = new ArrayList<>();
        Collections.addAll(invalid, fields);
        return new ValidationResult(false, "Validation: FAILED — " + reason, invalid);
    }

    public boolean isPassed() {
        return passed;
    }

    public String getMessage() {
        return message;
    }

    public List<String> getInvalidFields() {
        return invalidFields;
    }
}
