using System;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mars.Business;
namespace Mars.ViewModel
{
    public interface IProjectViewModel
    {
        bool SaveProject();
        ICommand SaveCommand { get; set; }
        ICommand ClearCommand { get; set; }

    }
}
