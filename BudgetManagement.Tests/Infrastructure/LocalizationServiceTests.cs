// Localization Service Tests - Comprehensive Albanian Translation Testing
// File: BudgetManagement.Tests/Infrastructure/LocalizationServiceTests.cs

using BudgetManagement.Shared.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;
using System.Threading;
using System.Windows;

namespace BudgetManagement.Tests.Infrastructure
{
    [TestClass]
    public class LocalizationServiceTests
    {
        private ILocalizationService? _localizationService;

        [TestInitialize]
        public void Setup()
        {
            // Initialize WPF Application for resource testing
            if (Application.Current == null)
            {
                new Application();
            }

            _localizationService = new LocalizationService();
        }

        [TestMethod]
        public void LocalizationService_DefaultLanguage_ShouldBeEnglish()
        {
            // Arrange & Act
            var currentCulture = _localizationService!.CurrentCulture;

            // Assert
            Assert.AreEqual("en", currentCulture.TwoLetterISOLanguageName);
        }

        [TestMethod]
        public void LocalizationService_SetLanguageToAlbanian_ShouldUpdateCulture()
        {
            // Arrange & Act
            _localizationService!.SetLanguage("sq");

            // Assert
            Assert.AreEqual("sq", _localizationService.CurrentCulture.TwoLetterISOLanguageName);
            Assert.AreEqual("sq", CultureInfo.DefaultThreadCurrentCulture?.TwoLetterISOLanguageName);
        }

        [TestMethod]
        public void LocalizationService_SetInvalidLanguage_ShouldFallbackToEnglish()
        {
            // Arrange & Act
            _localizationService!.SetLanguage("invalid");

            // Assert
            Assert.AreEqual("en", _localizationService.CurrentCulture.TwoLetterISOLanguageName);
        }

        [TestMethod]
        public void LocalizationHelper_ValidationMessages_ShouldReturnAlbanianText()
        {
            // Arrange
            LocalizationHelper.Initialize(_localizationService!);
            _localizationService.SetLanguage("sq");

            // Act
            var dateRequiredMessage = LocalizationHelper.ValidationMessages.DateRequired;
            var amountPositiveMessage = LocalizationHelper.ValidationMessages.AmountMustBePositive;

            // Assert
            Assert.IsTrue(!string.IsNullOrEmpty(dateRequiredMessage));
            Assert.IsTrue(!string.IsNullOrEmpty(amountPositiveMessage));
            // These should contain Albanian text, not English
            Assert.AreNotEqual("Date is required", dateRequiredMessage);
            Assert.AreNotEqual("Amount must be greater than zero", amountPositiveMessage);
        }

        [TestMethod]
        public void LocalizationHelper_ErrorMessages_ShouldReturnAlbanianText()
        {
            // Arrange
            LocalizationHelper.Initialize(_localizationService!);
            _localizationService.SetLanguage("sq");

            // Act
            var errorAddingIncomeMessage = LocalizationHelper.ErrorMessages.ErrorAddingIncome;
            var errorLoadingDataMessage = LocalizationHelper.ErrorMessages.ErrorLoadingInitialData;

            // Assert
            Assert.IsTrue(!string.IsNullOrEmpty(errorAddingIncomeMessage));
            Assert.IsTrue(!string.IsNullOrEmpty(errorLoadingDataMessage));
            // These should contain Albanian text, not English
            Assert.AreNotEqual("Error adding income", errorAddingIncomeMessage);
            Assert.AreNotEqual("Error loading initial data", errorLoadingDataMessage);
        }

        [TestMethod]
        public void LocalizationHelper_StatusMessages_ShouldReturnAlbanianText()
        {
            // Arrange
            LocalizationHelper.Initialize(_localizationService!);
            _localizationService.SetLanguage("sq");

            // Act
            var loadingIncomesMessage = LocalizationHelper.StatusMessages.LoadingIncomes;
            var operationCompletedMessage = LocalizationHelper.StatusMessages.OperationCompleted;

            // Assert
            Assert.IsTrue(!string.IsNullOrEmpty(loadingIncomesMessage));
            Assert.IsTrue(!string.IsNullOrEmpty(operationCompletedMessage));
            // These should contain Albanian text, not English
            Assert.AreNotEqual("Loading incomes...", loadingIncomesMessage);
            Assert.AreNotEqual("Operation completed", operationCompletedMessage);
        }

        [TestMethod]
        public void LocalizationService_GetFormattedString_ShouldFormatParameters()
        {
            // Arrange
            LocalizationHelper.Initialize(_localizationService!);
            var testKey = "TestFormattedString";
            Application.Current?.Resources.Add(testKey, "Hello {0}, you have {1} items");

            // Act
            var result = _localizationService!.GetFormattedString(testKey, "John", 5);

            // Assert
            Assert.AreEqual("Hello John, you have 5 items", result);
        }

        [TestMethod]
        public void LocalizationService_GetNonexistentKey_ShouldReturnDefaultValue()
        {
            // Arrange
            var nonexistentKey = "NonexistentKey123";
            var defaultValue = "Default Value";

            // Act
            var result = _localizationService!.GetString(nonexistentKey, defaultValue);

            // Assert
            Assert.AreEqual(defaultValue, result);
        }

        [TestMethod]
        public void LocalizationService_GetNonexistentKeyWithoutDefault_ShouldReturnKey()
        {
            // Arrange
            var nonexistentKey = "NonexistentKey123";

            // Act
            var result = _localizationService!.GetString(nonexistentKey);

            // Assert
            Assert.AreEqual(nonexistentKey, result);
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Reset culture to system default
            CultureInfo.DefaultThreadCurrentCulture = null;
            CultureInfo.DefaultThreadCurrentUICulture = null;
        }
    }

    [TestClass]
    public class LanguageManagerTests
    {
        private ILanguageManager? _languageManager;
        private ILocalizationService? _localizationService;

        [TestInitialize]
        public void Setup()
        {
            // Initialize WPF Application for resource testing
            if (Application.Current == null)
            {
                new Application();
            }

            _localizationService = new LocalizationService();
            _languageManager = new LanguageManager(_localizationService);
        }

        [TestMethod]
        public void LanguageManager_DefaultLanguage_ShouldBeEnglish()
        {
            // Assert
            Assert.AreEqual("en", _languageManager!.CurrentLanguage.Code);
            Assert.AreEqual("English", _languageManager.CurrentLanguage.DisplayName);
        }

        [TestMethod]
        public void LanguageManager_AvailableLanguages_ShouldContainEnglishAndAlbanian()
        {
            // Act
            var languages = _languageManager!.AvailableLanguages;

            // Assert
            Assert.AreEqual(2, languages.Length);
            Assert.IsTrue(Array.Exists(languages, l => l.Code == "en"));
            Assert.IsTrue(Array.Exists(languages, l => l.Code == "sq"));
        }

        [TestMethod]
        public void LanguageManager_ChangeLanguageToAlbanian_ShouldUpdateCurrentLanguage()
        {
            // Arrange
            bool languageChangedEventFired = false;
            _languageManager!.LanguageChanged += (s, e) => languageChangedEventFired = true;

            // Act
            _languageManager.ChangeLanguage("sq");

            // Assert
            Assert.AreEqual("sq", _languageManager.CurrentLanguage.Code);
            Assert.AreEqual("Shqip", _languageManager.CurrentLanguage.DisplayName);
            Assert.IsTrue(languageChangedEventFired);
        }

        [TestMethod]
        public void LanguageManager_ChangeToSameLanguage_ShouldNotFireEvent()
        {
            // Arrange
            bool languageChangedEventFired = false;
            _languageManager!.LanguageChanged += (s, e) => languageChangedEventFired = true;

            // Act - change to same language (English is default)
            _languageManager.ChangeLanguage("en");

            // Assert
            Assert.IsFalse(languageChangedEventFired);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void LanguageManager_ChangeToUnsupportedLanguage_ShouldThrowException()
        {
            // Act
            _languageManager!.ChangeLanguage("unsupported");
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Reset culture to system default
            CultureInfo.DefaultThreadCurrentCulture = null;
            CultureInfo.DefaultThreadCurrentUICulture = null;
        }
    }

    [TestClass]
    public class AlbanianTranslationCoverageTests
    {
        [TestMethod]
        public void AlbanianTranslations_AllValidationMessages_ShouldExist()
        {
            // This test verifies that all validation message keys exist in both resource files
            var requiredKeys = new[]
            {
                "DateRequired",
                "DateCannotBeFuture", 
                "DescriptionRequired",
                "DescriptionInvalidCharacters",
                "AmountMustBePositive",
                "AmountRequired",
                "CategoryIdRequired",
                "StartDateRequired",
                "EndDateRequired",
                "StartDateMustBeBeforeEndDate",
                "EndDateCannotBeFuture",
                "SearchPatternRequired",
                "SearchPatternTooShort",
                "DescriptionCannotBeEmpty",
                "CategoryNameRequired",
                "CategoryNameInvalidCharacters",
                "IdCannotBeEmpty",
                "CommandCannotBeNull",
                "QueryCannotBeNull"
            };

            // Initialize localization
            var localizationService = new LocalizationService();
            LocalizationHelper.Initialize(localizationService);

            foreach (var key in requiredKeys)
            {
                // Test English
                localizationService.SetLanguage("en");
                var englishValue = LocalizationHelper.GetString(key);
                Assert.AreNotEqual(key, englishValue, $"English translation missing for key: {key}");

                // Test Albanian
                localizationService.SetLanguage("sq");
                var albanianValue = LocalizationHelper.GetString(key);
                Assert.AreNotEqual(key, albanianValue, $"Albanian translation missing for key: {key}");
                Assert.AreNotEqual(englishValue, albanianValue, $"Albanian translation is same as English for key: {key}");
            }
        }

        [TestMethod]
        public void AlbanianTranslations_AllErrorMessages_ShouldExist()
        {
            var requiredKeys = new[]
            {
                "ErrorAddingIncome",
                "ErrorLoadingIncomes", 
                "ErrorSearchingIncomes",
                "ErrorLoadingInitialData",
                "ErrorAddingSpending",
                "ErrorLoadingSpendings",
                "ErrorSearchingSpendings",
                "ErrorLoadingCategories",
                "ErrorLoadingRecentSpendings",
                "ErrorLoadingRecentIncomes"
            };

            var localizationService = new LocalizationService();
            LocalizationHelper.Initialize(localizationService);

            foreach (var key in requiredKeys)
            {
                localizationService.SetLanguage("en");
                var englishValue = LocalizationHelper.GetString(key);
                Assert.AreNotEqual(key, englishValue, $"English translation missing for key: {key}");

                localizationService.SetLanguage("sq");
                var albanianValue = LocalizationHelper.GetString(key);
                Assert.AreNotEqual(key, albanianValue, $"Albanian translation missing for key: {key}");
                Assert.AreNotEqual(englishValue, albanianValue, $"Albanian translation is same as English for key: {key}");
            }
        }

        [TestMethod]
        public void AlbanianTranslations_AllStatusMessages_ShouldExist()
        {
            var requiredKeys = new[]
            {
                "LoadingIncomes",
                "LoadingSpendings",
                "LoadingCategories",
                "LoadingDashboard",
                "LoadingAnalytics",
                "ProcessingRequest",
                "SavingChanges",
                "OperationCompleted",
                "OperationFailed",
                "NoDataAvailable",
                "DataLoadedSuccessfully"
            };

            var localizationService = new LocalizationService();
            LocalizationHelper.Initialize(localizationService);

            foreach (var key in requiredKeys)
            {
                localizationService.SetLanguage("en");
                var englishValue = LocalizationHelper.GetString(key);
                Assert.AreNotEqual(key, englishValue, $"English translation missing for key: {key}");

                localizationService.SetLanguage("sq");
                var albanianValue = LocalizationHelper.GetString(key);
                Assert.AreNotEqual(key, albanianValue, $"Albanian translation missing for key: {key}");
                Assert.AreNotEqual(englishValue, albanianValue, $"Albanian translation is same as English for key: {key}");
            }
        }
    }
}