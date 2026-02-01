using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;

namespace Mars.Helpers
{
    public class ValidatedObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [RangeValidator(10, RangeBoundaryType.Inclusive, 20, RangeBoundaryType.Inclusive, MessageTemplate = "invalid int")]
        public int ValidatedIntProperty { get; set; }

        [RegexValidator(@"^a*$", MessageTemplate = "invalid string")]
        public string ValidatedStringProperty { get; set; }

        public int NonValidatedProperty { get; set; }

        [RegexValidator(@"^a*$", MessageTemplate = "invalid string: vab")]
        [StringLength(2, ErrorMessage = "invalid string: data annotations")]
        public string MultipleSourceValidatedStringProperty { get; set; }

        [RegexValidator(@"^a*$", MessageTemplate = "invalid string default")]
        [RegexValidator(@"^a*$", MessageTemplate = "invalid string ruleset", Ruleset = "A")]
        public string MultipleRulesetValidatedStringProperty { get; set; }

        private string twoWayValidatedStringProperty;

        //[StringLengthValidator(1, MessageTemplate = "String must be one character")]
        [StringLength(1, ErrorMessage = "String must be one character")]
        public string TwoWayValidatedStringProperty
        {
            get { return twoWayValidatedStringProperty; }
            set
            {
                twoWayValidatedStringProperty = value;
                OnPropertyChanged("TwoWayValidatedStringProperty");
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var changedEvent = PropertyChanged;
            if (changedEvent != null)
            {
                changedEvent(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
