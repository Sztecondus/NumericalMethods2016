using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Gu.Wpf.DataGrid2D;
using Microsoft.Win32;
using NumMethods1.Utils;
using NumMethods2.Exceptions;
using NumMethods2.MatrixMath;

namespace NumMethods2.ViewModel
{
    public interface IMainPageViewInteraction
    {
        DataGrid MatrixGrid { get; }
        DataGrid ResGrid { get; }
    }

    public class MainViewModel : ViewModelBase
    {
        private ICommand _addMatrixRowCommand;

        private ICommand _changeLanguageCommand;

        private AvailableLocale _currentLocale;

        private int _currentlySelectedArraySizeIndex;

        private bool _enableDebugLog;

        private ICommand _extendMatrixCommand;

        /// <summary>
        ///     Item 1 is matrix snapshot , Item 2 is results snapshot prepared for UI display.
        /// </summary>
        private List<Tuple<double[,], double[,]>> _iterationLog;

        private string _langImgSource;

        private ICommand _loadFromFileCommand;
        private Dictionary<string, string> _locale;

        private double[,] _matrixBackup;

        private List<double> _matrixSolutions = new List<double>();

        private double[,] _resultsGrid = {{0}, {0}};

        private ICommand _submitDataCommand;

        public MainViewModel()
        {
            CurrentLocale = AvailableLocale.EN;
        }

        public Dictionary<string, string> Locale
        {
            get { return _locale; }
            set
            {
                _locale = value;
                RaisePropertyChanged(() => Locale);
            }
        }

        public string LangImgSourceBind
        {
            get { return _langImgSource; }
            set
            {
                _langImgSource = $@"../Localization/{value}.png";
                RaisePropertyChanged(() => LangImgSourceBind);
            }
        }

        public AvailableLocale CurrentLocale
        {
            get { return _currentLocale; }
            set
            {
                _currentLocale = value;
                switch (value)
                {
                    case AvailableLocale.PL:
                        Locale = LocalizationManager.PlDictionary;
                        break;
                    case AvailableLocale.EN:
                        Locale = LocalizationManager.EnDictionary;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);
                }
                //sets flag of next locale
                LangImgSourceBind = LocalizationManager.GetNextLocale(value).ToString();
            }
        }

        public ICommand ChangeLanguageCommand =>
            _changeLanguageCommand ?? (_changeLanguageCommand = new RelayCommand(() =>
            {
                if ((int) CurrentLocale == Enum.GetNames(typeof (AvailableLocale)).Length - 1)
                    CurrentLocale = 0;
                else
                    CurrentLocale += 1;
            }));

        public ICommand ExtendMatrixCommand =>
            _extendMatrixCommand ?? (_extendMatrixCommand = new RelayCommand(ExtendMatrix));

        public ICommand SubmitDataCommand =>
            _submitDataCommand ?? (_submitDataCommand = new RelayCommand(DoMaths));

        public object LoadFromFileCommand =>
            _loadFromFileCommand ?? (_loadFromFileCommand = new RelayCommand(LoadFromFile));

        public int CurrentlySelectedArraySizeIndex
        {
            get { return _currentlySelectedArraySizeIndex; }
            set
            {
                _currentlySelectedArraySizeIndex = value;
                Matrix = new double[PossibleArraySizes[value], PossibleArraySizes[value]];
                ResultsGrid = new double[PossibleArraySizes[value], 1];
                Size = PossibleArraySizes[value];
                RaisePropertyChanged(() => CurrentlySelectedArraySizeIndex);
            }
        }

        public List<int> PossibleArraySizes { get; } = new List<int>
        {
            2,
            3,
            4,
            5,
            6,
            7,
            8,
            9,
            10
        };

        public bool EnableDebugLog
        {
            get { return _enableDebugLog; }
            set
            {
                _enableDebugLog = value;
                RaisePropertyChanged(() => EnableDebugLog);
            }
        }

        /// <summary>
        ///     Item 1 is matrix snapshot ,
        ///     Item 2 is results snapshot ,
        ///     Item 3 is iteration counter
        /// </summary>
        public ObservableCollection<Tuple<double[,], double[,], string>> IterationLog
        {
            get
            {
                if (_iterationLog == null)
                    return new ObservableCollection<Tuple<double[,], double[,], string>>();
                var output = new ObservableCollection<Tuple<double[,], double[,], string>>();
                for (var i = 0; i < _iterationLog.Count; i++)
                {
                    output.Add(new Tuple<double[,], double[,], string>(_iterationLog[i].Item1, _iterationLog[i].Item2,
                        (i + 1).ToString()));
                }
                return output;
            }
        }


        public IMainPageViewInteraction View { get; set; }

        public int Size { get; set; } = 2;
        private double[,] _matrix { get; set; } = {{0, 0}, {0, 0}};

        public double[,] Matrix
        {
            get { return _matrix; }
            set
            {
                _matrix = value;
                MatrixSolutions = new List<double>();
                RaisePropertyChanged(() => Matrix);
            }
        }

        public double[,] ResultsGrid
        {
            get { return _resultsGrid; }
            set
            {
                _resultsGrid = value;
                RaisePropertyChanged(() => ResultsGrid);
            }
        }

        public List<double> MatrixSolutions
        {
            get { return _matrixSolutions; }
            set
            {
                _matrixSolutions = value;
                RaisePropertyChanged(() => MatrixSolutions);
            }
        }

        public void ExtendMatrix()
        {
            var list = new List<List<double>>();
            var flatMatrix = Matrix.Cast<double>();
            for (var i = 0; i < Size; i++)
            {
                var row = flatMatrix.Skip(i*Size).Take(Size).ToList();
                row.Add(0);
                list.Add(row);
            }
            var newRow = new List<double>();
            for (var i = 0; i < Size; i++)
                newRow.Add(0);
            list.Add(newRow);
            Size++;
            ResultsGrid = Utils.ResizeArray(ResultsGrid, Size, 1);
            Matrix = list.To2DArray();
        }

        private void LoadFromFile()
        {
            var fp = new OpenFileDialog();
            if (fp.ShowDialog() ?? false)
                using (var reader = new StreamReader(fp.OpenFile()))
                {
                    LoadMatrix(reader);
                }
        }

        public void LoadFromFile(string path)
        {
            using (var reader = new StreamReader(path))
            {
                try
                {
                    LoadMatrix(reader);
                }
                catch (InvalidMatrixFileException)
                {
                    //
                }
            }
        }

        private void LoadMatrix(StreamReader reader)
        {
            try
            {
                var data = MatrixFileLoadingManager.LoadData(reader.ReadToEnd());
                Matrix = data.Item1;
                ResultsGrid = data.Item2;
                Size = Matrix.GetLength(0);
            }
            catch (InvalidMatrixFileException)
            {
                MessageBox.Show(Locale["#FileParseExceptionMsg"], Locale["#FileParseExceptionTitle"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        ///     Initializes matrix solution.
        /// </summary>
        private async void DoMaths()
        {
            _matrix = (double[,]) View.MatrixGrid.GetArray2D();
            _resultsGrid = (double[,]) View.ResGrid.GetArray2D();
            _iterationLog = EnableDebugLog ? new List<Tuple<double[,], double[,]>>() : null;
            MatrixSolutions = new List<double>();
            await Task.Run(() =>
            {
                try
                {
                    MatrixSolutions =
                        NumCore.FindMatrixSolutions((double[,]) _matrix.Clone(), (double[,]) _resultsGrid.Clone(),
                            ref _iterationLog);
                }
                catch (NoSystemSolutionsException)
                {
                    MessageBox.Show(Locale["#NoSolutionExceptionMsg"], Locale["#NoSolutionExceptionTitle"],
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (InfiniteSystemSolutionsException)
                {
                    MessageBox.Show(Locale["#InfSolutionExceptionMsg"], Locale["#InfSolutionExceptionTitle"],
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception)
                {
                    MessageBox.Show(Locale["#UnexpectedExceptionMsg"], Locale["#UnexpectedExceptionTitle"],
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            });
            RaisePropertyChanged(() => IterationLog);
        }
    }
}