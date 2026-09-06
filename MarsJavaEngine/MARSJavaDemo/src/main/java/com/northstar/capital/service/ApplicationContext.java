package com.northstar.capital.service;

import com.northstar.capital.model.AppEnvironment;

import java.util.ArrayList;
import java.util.List;
import java.util.function.Consumer;

public class ApplicationContext {
    public static final String APP_NAME = "Northstar Capital Markets Workstation";
    public static final String SHORT_NAME = "Northstar CM";
    public static final String VERSION = "1.0.0";
    public static final String SERVER = "NCM-UAT-01";

    private final DemoRepository repository;
    private final ReferenceDataService referenceDataService;
    private final ValidationService validationService;
    private final TradeService tradeService;
    private final List<Consumer<String>> tradeListeners = new ArrayList<>();

    private AppEnvironment environment = AppEnvironment.UAT;
    private String currentUser = "TRADER01";
    private String statusMessage = "Ready";

    public ApplicationContext() {
        this.repository = DemoDataFactory.create();
        this.referenceDataService = new ReferenceDataService(repository);
        this.validationService = new ValidationService();
        this.tradeService = new TradeService(repository, validationService, referenceDataService);
    }

    public DemoRepository getRepository() {
        return repository;
    }

    public ReferenceDataService getReferenceDataService() {
        return referenceDataService;
    }

    public ValidationService getValidationService() {
        return validationService;
    }

    public TradeService getTradeService() {
        return tradeService;
    }

    public AppEnvironment getEnvironment() {
        return environment;
    }

    public void setEnvironment(AppEnvironment environment) {
        this.environment = environment == null ? AppEnvironment.UAT : environment;
    }

    public String getCurrentUser() {
        return currentUser;
    }

    public void setCurrentUser(String currentUser) {
        this.currentUser = currentUser;
    }

    public String getStatusMessage() {
        return statusMessage;
    }

    public void setStatusMessage(String statusMessage) {
        this.statusMessage = statusMessage == null ? "" : statusMessage;
    }

    public String getServerName() {
        return environment == AppEnvironment.PROD ? "NCM-PROD-01"
                : environment == AppEnvironment.DEV ? "NCM-DEV-01" : SERVER;
    }

    public void addTradeListener(Consumer<String> listener) {
        tradeListeners.add(listener);
    }

    public void notifyTradesChanged(String reason) {
        for (Consumer<String> listener : tradeListeners) {
            listener.accept(reason);
        }
    }
}
