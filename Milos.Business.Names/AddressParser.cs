using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Runtime.Serialization;
using CODE.Framework.Fundamentals.Configuration;
using CODE.Framework.Fundamentals.Utilities;
using Milos.Data;

namespace Milos.Business.Names
{
    /// <summary>
    /// This Address Parser has been mainly written based on the one from SFDCT.
    /// Things that should change very soon on about it:
    /// 1. Define an Interface for address parsers.
    /// 2. Define an abstract class that implements that interface.
    /// 3. Define a USAddressParser inheriting from the class above and move the code below over there.
    /// </summary>
    public class AddressParser
    {
        /// <summary>
        /// Is the address invalid
        /// </summary>
        private bool invalidAddress;

        /// <summary>
        /// For internal use only
        /// </summary>
        private readonly List<CountryInformation> countryInfo = new List<CountryInformation>();

        /// <summary>
        /// For internal use only
        /// </summary>
        private string state = string.Empty;

        /// <summary>
        /// For internal use only
        /// </summary>
        private string zip = string.Empty;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public AddressParser()
        {
            LoadCountries();
        }

        /// <summary>
        /// LoadCountries: grab the countries from the database and load in an ArrayList.
        /// </summary>
        private void LoadCountries()
        {
            using (var boCountry = GetCountryBusinessObject())
            using (var dsCountries = boCountry.GetList())
            {
                countryInfo.Clear();
                foreach (DataRow row in dsCountries.Tables["Country"].Rows)
                    countryInfo.Add(new CountryInformation(row["cName"].ToString(), row["cCode"].ToString(), (AddressFormat) row["iAddrFormat"]));
            }
        }

        /// <summary>
        /// Returns an instance of the country business object.
        /// </summary>
        /// <returns>Country business object</returns>
        /// <remarks>This method is designed to be overriden in subclasses to use different country business objects.</remarks>
        protected virtual CountryBusinessObject GetCountryBusinessObject() => new CountryBusinessObject();

        /// <summary>
        /// Get the City from a given address' line.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        protected string GetCity(string line)
        {
            if (line == null) throw new ArgumentNullException("line");
            // It may have just the city, without the state, the zip, or even without a ","
            // If there's no "," in this line, we may ended up with something like "HOUSTON TX 77333". Let's get rid of the State and Zip.
            return line.IndexOf(",", StringComparison.Ordinal) >= 0 ? line.Substring(0, StringHelper.At(",", line) - 1).Trim() : line.Replace(state + " " + zip, string.Empty).Trim();
        }

        /// <summary>
        /// Get the State from a given address' line.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        protected string GetState(string line)
        {
            if (line == null) throw new ArgumentNullException("line");
            var retVal = string.Empty;

            if (line.IndexOf(",", StringComparison.Ordinal) >= 0)
            {
                // The zip is after the first ","
                retVal = line.Substring(StringHelper.At(",", line) + 1).Trim();

                if (retVal.IndexOf(",", StringComparison.Ordinal) >= 0)
                    // Theoretically, there should be the state after the next ","
                    // otherwise we must look for the last space.
                    retVal = retVal.Substring(0, StringHelper.At(",", retVal) - 1).Trim();
                else if (retVal.IndexOf(" ", StringComparison.Ordinal) >= 0)
                    // If we do not have another "," and no spaces, the address might be invalid...
                    retVal = retVal.Substring(0, StringHelper.At(" ", retVal, StringHelper.Occurs(" ", retVal)) - 1).Trim();
            }
            else
            {
                //TODO: Handle the following scenario:
                //      if the line for city, state, zipcode, only has something like 
                //      SAN ANTONIO TX
                //      (city with more than one word plus the state, parser gets messed up.
                //		It works fine if it has the comma, though (as in "SAN ANTONIO, TX")

                // We may not have any "," within the line (like "HOUSTON TX 123456"
                if (line.IndexOf(" ", StringComparison.Ordinal) >= 0) retVal = line.Substring(0, StringHelper.At(" ", line, StringHelper.Occurs(" ", line)));

                // If length > 2, this is too big for a state, so it may has the city with it. Let's get just what 
                // must be the state.
                if (retVal.Length > 2)
                {
                    var words = (int)StringHelper.GetWordCount(retVal);
                    retVal = StringHelper.GetWordNumb(retVal, words);
                }
            }

            state = retVal;

            return retVal;
        }

        /// <summary>
        /// Get the Zip Code from a given address' line.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        protected string GetZip(string line)
        {
            if (line == null) throw new ArgumentNullException("line");
            var retVal = string.Empty;

            if (StringHelper.Occurs(",", line) > 1)
                // The state should be after the second ","
                retVal = line.Substring(StringHelper.At(",", line, 2) + 1).Trim();
            else if (line.IndexOf(" ", StringComparison.Ordinal) >= 0) // Maybe the zip code is separated by a space...
            {
                retVal = line;
                retVal = retVal.Substring(StringHelper.At(" ", retVal, StringHelper.Occurs(" ", retVal))).Trim();
            }

            // If a ZIP code wasn't passed, we have to handle it.
            if (retVal == state) retVal = string.Empty;

            zip = retVal;

            return retVal;
        }

        /// <summary>
        /// Get City, Zip, and State from a given address line.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="data"></param>
        private void GetCityZipState(string line, Address data)
        {
            if (line == null) throw new ArgumentNullException("line");
            if (data == null) throw new ArgumentNullException("data");

            data.State = GetState(line);
            if (string.IsNullOrEmpty(data.State)) invalidAddress = true;

            data.Zip = GetZip(line);
            if (string.IsNullOrEmpty(data.Zip)) invalidAddress = true;

            data.City = GetCity(line);
            if (string.IsNullOrEmpty(data.City)) invalidAddress = true;
        }

        /// <summary>
        /// Get Zip, City, and State from a given address line.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="data"></param>
        private void GetZipCityState(string line, Address data)
        {
            if (line == null) throw new ArgumentNullException("line");
            if (data == null) throw new ArgumentNullException("data");
            line = line.Trim();

            // Zip/ Postal code
            data.Zip = line.Substring(0, line.IndexOf(" ", StringComparison.Ordinal)).Trim();
            if (string.IsNullOrEmpty(data.Zip)) invalidAddress = true;
            line = line.Substring(line.IndexOf(" ", StringComparison.Ordinal)).Trim();

            // State
            var internalState = string.Empty;
            if (line.IndexOf(",", StringComparison.Ordinal) > -1)
            {
                internalState = line.Substring(line.IndexOf(",", StringComparison.Ordinal) + 1).Trim();
                line = line.Substring(0, line.IndexOf(",", StringComparison.Ordinal));
            }
            else
            {
                if (line.IndexOf(" ", StringComparison.Ordinal) > -1)
                {
                    var occurs = StringHelper.Occurs(" ", line);
                    var at = StringHelper.At(" ", line, occurs);
                    internalState = line.Substring(at);
                    line = line.Substring(0, at);
                }
            }

            data.State = internalState;
            if (string.IsNullOrEmpty(data.State)) invalidAddress = true;

            // City
            data.City = line.Trim();
            if (string.IsNullOrEmpty(data.City)) invalidAddress = true;
        }

        /// <summary>
        /// Get Postal code and city for a given address line
        /// </summary>
        /// <param name="line"></param>
        /// <param name="data"></param>
        private void GetPostalCodeCity(string line, Address data)
        {
            if (line == null) throw new ArgumentNullException("line");
            if (data == null) throw new ArgumentNullException("data");
            var space = line.IndexOf(" ", StringComparison.Ordinal);

            data.Zip = line.Substring(0, space).Trim();
            if (string.IsNullOrEmpty(data.Zip)) invalidAddress = true;

            data.City = line.Substring(space).Trim();
            if (string.IsNullOrEmpty(data.City)) invalidAddress = true;
        }

        /// <summary>
        /// Get Postal code and city for a given address line
        /// </summary>
        /// <param name="line"></param>
        /// <param name="data"></param>
        private void GetCityPostalCode(string line, Address data)
        {
            if (line == null) throw new ArgumentNullException("line");
            if (data == null) throw new ArgumentNullException("data");
            if (line.IndexOf(",", StringComparison.Ordinal) == -1)
            {
                // No comma, so we have to parse hard-core
                var spaces = StringHelper.Occurs(' ', line);
                var space = StringHelper.At(" ", line, spaces);

                data.City = line.Substring(0, space).Trim();
                if (string.IsNullOrEmpty(data.City)) invalidAddress = true;

                data.Zip = line.Substring(space).Trim();
                if (string.IsNullOrEmpty(data.Zip)) invalidAddress = true;
            }
            else
            {
                // We have a comma, so we use that as the separator
                var space = StringHelper.At(",", line);

                data.City = line.Substring(0, space - 1).Trim();
                if (string.IsNullOrEmpty(data.City)) invalidAddress = true;

                data.Zip = line.Substring(space).Trim();
                if (string.IsNullOrEmpty(data.Zip)) invalidAddress = true;
            }
        }

        /// <summary>
        /// Parses address.
        /// </summary>
        /// <remarks>
        /// If the parser is not able to parse a given address, it's gonna raise the InvalidAddress event, 
        /// which in turn will have Args holding the source and address object so that the client
        /// will be able to analyze the passed string (source) and the best guess made by the parser (Address object).
        /// </remarks>
        /// <param name="address">The string containing full address to be parsed.</param>
        /// <returns>Address object containing the parsed address</returns>
        public Address ParseAddress(string address)
        {
            if (address == null) throw new ArgumentNullException("address");
            var ad = new Address();
            ParseAddress(address, ad);
            return ad;
        }

        /// <summary>
        /// Parses address.
        /// </summary>
        /// <remarks>
        /// If the parser is not able to parse a given address, it's gonna raise the InvalidAddress event, 
        /// which in turn will have Args holding the source and address object so that the client
        /// will be able to analyze the passed string (source) and the best guess made by the parser (Address object).
        /// </remarks>
        /// <param name="address">The string containing full address to be parsed.</param>
        /// <param name="resultingAddress">Address object that will keep the parsed address.</param>
        public void ParseAddress(string address, Address resultingAddress)
        {
            if (address == null) throw new ArgumentNullException("address");
            if (resultingAddress == null) throw new ArgumentNullException("resultingAddress");
            // We also consider some current system defaults
            var regionCode3 = RegionInfo.CurrentRegion.ThreeLetterWindowsRegionName;
            if (ConfigurationSettings.Settings.IsSettingSupported("address:ThreeLetterRegionName")) regionCode3 = ConfigurationSettings.Settings["address:ThreeLetterRegionName"];
            var regionCode2 = RegionInfo.CurrentRegion.TwoLetterISORegionName;
            if (ConfigurationSettings.Settings.IsSettingSupported("address:TwoLetterISORegionName")) regionCode2 = ConfigurationSettings.Settings["address:TwoLetterISORegionName"];

            ParseAddress(address, resultingAddress, regionCode3, regionCode2);
        }

        /// <summary>
        /// Parses address.
        /// </summary>
        /// <remarks>
        /// If the parser is not able to parse a given address, it's gonna raise the InvalidAddress event, 
        /// which in turn will have Args holding the source and address object so that the client
        /// will be able to analyze the passed string (source) and the best guess made by the parser (Address object).
        /// </remarks>
        /// <param name="address">The string containing full address to be parsed.</param>
        /// <param name="threeLetterRegionCode">Three letter region code (such as "USA" or "AUT") that is to be assumed if no country is specified.</param>
        /// <param name="twoLetterIsoRegionCode">Two letter ISO region code (such as "US" or "AT") that is to be assumed if no country is specified.</param>
        /// <returns>Parsed address object</returns>
        public Address ParseAddress(string address, string threeLetterRegionCode, string twoLetterIsoRegionCode)
        {
            if (address == null) throw new ArgumentNullException("address");
            if (threeLetterRegionCode == null) throw new ArgumentNullException("threeLetterRegionCode");
            if (twoLetterIsoRegionCode == null) throw new ArgumentNullException("twoLetterIsoRegionCode");

            var address2 = new Address();
            ParseAddress(address, address2, threeLetterRegionCode, twoLetterIsoRegionCode);
            return address2;
        }

        /// <summary>
        /// Parses address.
        /// </summary>
        /// <remarks>
        /// If the parser is not able to parse a given address, it's gonna raise the InvalidAddress event, 
        /// which in turn will have Args holding the source and address object so that the client
        /// will be able to analyze the passed string (source) and the best guess made by the parser (Address object).
        /// </remarks>
        /// <param name="address">The string containing full address to be parsed.</param>
        /// <param name="resultingAddress">Address object that will keep the parsed address.</param>
        /// <param name="threeLetterRegionCode">Three letter region code (such as "USA" or "AUT") that is to be assumed if no country is specified.</param>
        /// <param name="twoLetterIsoRegionCode">Two letter ISO region code (such as "US" or "AT") that is to be assumed if no country is specified.</param>
        public void ParseAddress(string address, Address resultingAddress, string threeLetterRegionCode, string twoLetterIsoRegionCode)
        {
            if (address == null) throw new ArgumentNullException("address");
            if (resultingAddress == null) throw new ArgumentNullException("resultingAddress");
            if (threeLetterRegionCode == null) throw new ArgumentNullException("threeLetterRegionCode");
            if (twoLetterIsoRegionCode == null) throw new ArgumentNullException("twoLetterIsoRegionCode");

            address = address.Trim();

            invalidAddress = false;

            var lines = address.Split('\r');

            // If there's just one line in the source, we may just have the Address (without City, State and so on), 
            // therefore we should consider that line as the address, and don't need to do any further proccessing.
            // This is a little better than Outlook, which messes up with address that has just one line.
            if (lines.Length == 1)
            {
                resultingAddress.Address1 = lines[0];
                resultingAddress.Country = threeLetterRegionCode;
                invalidAddress = true;
            }
            else
            {
                // The first thing we try to figure out is which address type this might be, based on the country
                // To do so, we look at the last line to see whether it appears to be a country
                var af = GetAddressFormatForCountry(lines[lines.Length - 1], threeLetterRegionCode, twoLetterIsoRegionCode);

                // Depending on the address format we assume as the starting point, we use different parsing algorithms
                switch (af)
                {
                    case AddressFormat.CityStateZip:
                        // Typical US parsing
                        ParseCityStateZipAddress(lines, resultingAddress);
                        break;
                    case AddressFormat.PostalCodeCity:
                        // Typical European parsing
                        ParsePostalCodeCityAddress(lines, resultingAddress);
                        break;
                    case AddressFormat.CityPostalCode:
                        // Countries such as Taiwan
                        ParseCityPostalCodeAddress(lines, resultingAddress);
                        break;
                    case AddressFormat.PostalCodeCityState:
                        // Can't recall which countries us this, but they exist <s>
                        ParsePostalCodeCityStateAddress(lines, resultingAddress);
                        break;
                }
            }

            // We make sure we have appropriate country information
            if (address.Length > 0 && resultingAddress.Country.Length == 0 || string.Compare(resultingAddress.Country, threeLetterRegionCode, true, CultureInfo.InvariantCulture) == 0)
            {
                var countryName = string.Empty;
                foreach (var info in countryInfo)
                    if (string.Compare(info.IsoCode, twoLetterIsoRegionCode, true, CultureInfo.InvariantCulture) == 0)
                    {
                        countryName = info.Name;
                        break;
                    }

                // The following IF statement ended up using an unassigned field, so it could never be true.
                //if (this.bNewAddress)
                //{
                //    // No country was assigned
                //    addressObject.Country = countryName;
                //    addressString = addressString.Replace(threeLetterRegionCode, countryName);
                //}
                //else
                //{
                resultingAddress.Country = countryName;
                //}
            }

            // We check if the country is valid...
            //this.LoadCountries(); // Changed by Markus: Countries must already be loaded at this point. They load on init.

            // We scan the array to see if the selected country is valid...
            if (!invalidAddress)
            {
                // The address appears to be valid so far, but we double check to make
                // sure that all the above parsing has not missed an invalid country name
                var foundCountry = false;
                foreach (var info in countryInfo)
                    if (string.Compare(info.Name, resultingAddress.Country, true, CultureInfo.InvariantCulture) == 0)
                    {
                        foundCountry = true;
                        break;
                    }

                if (!foundCountry) invalidAddress = true;
            }

            // Now, we make sure we actually ended up with an address we were able to parse
            // If not, we raise an event.
            if (invalidAddress)
                // We raise the InvalidAddress event. That way, each different type of UI will be able to handle the implementation.
                OnInvalidAddress(new InvalidAddressEventArgs {Address = resultingAddress, Source = address, Updated = true});
        }

        /// <summary>
        /// Parses an address that is expected to be formatted as Postal Code-City
        /// Example:
        /// Hochtennstrasse 3b
        /// Top 5
        /// 5700 Zell am See
        /// Austria
        /// </summary>
        /// <param name="lines">Array of individual address lines</param>
        /// <param name="address">Address object that is to be populated</param>
        public virtual void ParsePostalCodeCityAddress(string[] lines, Address address)
        {
            if (address == null) throw new ArgumentNullException("address");

            var lineNumber = 0;
            // Iterate through lines from the last one to the first one.
            for (var counter = lines.Length; counter > 0; counter--)
            {
                var line = lines[counter - 1];

                if (line.Length <= 0) continue;
                lineNumber++;
                switch (lineNumber)
                {
                    case 1:
                        // This should be either the country or the the postal code and city
                        if (line.IndexOf(",", StringComparison.Ordinal) < 0)
                        {
                            // This should be the country...
                            address.Country = line.Trim();

                            // Let's search this country in the list...
                            var foundCountry = false;
                            foreach (var info in countryInfo)
                                if (string.Compare(info.Name, address.Country, true, CultureInfo.InvariantCulture) == 0)
                                {
                                    foundCountry = true;
                                    break;
                                }

                            if (!foundCountry)
                            {
                                // Didn't find it? So, this is not the country... it may be the "postal code and city" line.
                                address.Country = string.Empty;

                                // Let's try...
                                GetPostalCodeCity(line, address);
                            }

                            //this.country = addressObject.Country;
                        }
                        else
                            // This should be the line with "City, ST Zip"...
                            GetPostalCodeCity(line, address);

                        break;

                    case 2:
                        if (address.Country.Length == 0)
                        {
                            // The last line was not the country, so it was the city and therefore this must be one of the 3 address' line.
                            if (counter == 3)
                                invalidAddress = true;
                            else
                            {
                                if (counter == 1) address.Address1 = line.Trim();
                                else if (counter == 2) address.Address2 = line.Trim();
                                else if (counter == 3) address.Address3 = line.Trim();
                            }
                        }
                        else
                            // this may be the "City, ST Zip" line, so let's start from right to left.
                            GetPostalCodeCity(line, address);

                        break;

                    default:
                        if (counter > 3)
                            invalidAddress = true;
                        else
                        {
                            if (counter == 1) address.Address1 = line.Trim();
                            else if (counter == 2) address.Address2 = line.Trim();
                            else if (counter == 3) address.Address3 = line.Trim();
                        }

                        break;
                }
            }
        }

        /// <summary>
        /// Parses an address that is expected to be formatted as City-Postal Code
        /// </summary>
        /// <param name="lines">Array of individual address lines</param>
        /// <param name="address">Address object that is to be populated</param>
        public virtual void ParseCityPostalCodeAddress(string[] lines, Address address)
        {
            if (address == null) throw new ArgumentNullException("address");

            var lineCount = 0;
            // Iterate through lines from the last one to the first one.
            for (var counter = lines.Length; counter > 0; counter--)
            {
                var line = lines[counter - 1];

                if (line.Length <= 0) continue;

                lineCount++;
                switch (lineCount)
                {
                    case 1:
                        // This should be either the country or the the postal code and city
                        if (line.IndexOf(",", StringComparison.Ordinal) < 0)
                        {
                            // This should be the country...
                            address.Country = line.Trim();

                            // Let's search this country in the list...
                            var foundCountry = false;
                            foreach (var info in countryInfo)
                                if (string.Compare(info.Name, address.Country, true, CultureInfo.InvariantCulture) == 0)
                                {
                                    foundCountry = true;
                                    break;
                                }

                            if (!foundCountry)
                            {
                                // Didn't find it? So, this is not the country... it may be the "postal code and city" line.
                                address.Country = string.Empty;

                                // Let's try...
                                GetCityPostalCode(line, address);
                            }

                            //this.country = addressObject.Country;
                        }
                        else
                            // This should be the line with "City, ST Zip"...
                            GetCityPostalCode(line, address);

                        break;

                    case 2:
                        if (address.Country.Length == 0)
                        {
                            // The last line was not the country, so it was the city and therefore this must be one of the 3 address' line.
                            if (counter == 3)
                                invalidAddress = true;
                            else
                            {
                                if (counter == 1) address.Address1 = line.Trim();
                                else if (counter == 2) address.Address2 = line.Trim();
                                else if (counter == 3) address.Address3 = line.Trim();
                            }
                        }
                        else
                            // this may be the "City, ST Zip" line, so let's start from right to left.
                            GetCityPostalCode(line, address);

                        break;

                    default:
                        if (counter > 3)
                            invalidAddress = true;
                        else
                        {
                            if (counter == 1) address.Address1 = line.Trim();
                            else if (counter == 2) address.Address2 = line.Trim();
                            else if (counter == 3) address.Address3 = line.Trim();
                        }

                        break;
                }
            }
        }

        /// <summary>
        /// Parses an address that is expected to be formatted as City-State-ZIP
        /// Example:
        /// 6605 Cypresswood Dr.
        /// Spring, TX 77388
        /// United States
        /// </summary>
        /// <param name="lines">Array of individual address lines</param>
        /// <param name="address">Address object that is to be populated</param>
        public virtual void ParseCityStateZipAddress(string[] lines, Address address)
        {
            if (address == null) throw new ArgumentNullException("address");

            var lineNumber = 0;
            // Iterate through lines from the last one to the first one.
            for (var counter = lines.Length; counter > 0; counter--)
            {
                var line = lines[counter - 1];

                if (line.Length > 0)
                {
                    lineNumber++;
                    switch (lineNumber)
                    {
                        case 1:
                            // This should be either the country or the city, state and zip...
                            if (line.IndexOf(",", StringComparison.Ordinal) < 0)
                            {
                                // This should be the country...
                                address.Country = line.Trim();

                                // Let's search this country in the list...
                                var foundCountry = false;
                                foreach (var info in countryInfo)
                                    if (string.Compare(info.Name, address.Country, true, CultureInfo.InvariantCulture) == 0)
                                    {
                                        foundCountry = true;
                                        break;
                                    }

                                if (!foundCountry)
                                {
                                    // Didn't find it? So, this is not the country... it may be the "City, ST, Zip" line.
                                    address.Country = string.Empty;

                                    // Let's try...
                                    GetCityZipState(line, address);
                                }

                                //this.country = addressObject.Country;
                            }
                            else
                                // This should be the line with "City, ST Zip"...
                                GetCityZipState(line, address);

                            break;

                        case 2:
                            if (address.Country.Length == 0)
                            {
                                // The last line was not the country, so it was the city and therefore this must be one of the 3 address' line.
                                if (counter == 3)
                                    invalidAddress = true;
                                else
                                {
                                    if (counter == 1) address.Address1 = line.Trim();
                                    else if (counter == 2) address.Address2 = line.Trim();
                                    else if (counter == 3) address.Address3 = line.Trim();
                                }
                            }
                            else
                                // this may be the "City, ST Zip" line, so let's start from right to left.
                                GetCityZipState(line, address);

                            break;

                        default:
                            if (counter > 3)
                                invalidAddress = true;
                            else
                            {
                                if (counter == 1) address.Address1 = line.Trim();
                                else if (counter == 2) address.Address2 = line.Trim();
                                else if (counter == 3) address.Address3 = line.Trim();
                            }

                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Parses an address that is expected to be formatted as ZIP-City-State
        /// </summary>
        /// <param name="lines">Array of individual address lines</param>
        /// <param name="address">Address object that is to be populated</param>
        public virtual void ParsePostalCodeCityStateAddress(string[] lines, Address address)
        {
            if (address == null) throw new ArgumentNullException("address");

            var lineNumber = 0;
            // Iterate through lines from the last one to the first one.
            for (var counter = lines.Length; counter > 0; counter--)
            {
                var line = lines[counter - 1];

                if (line.Length > 0)
                {
                    lineNumber++;
                    switch (lineNumber)
                    {
                        case 1:
                            // This should be either the country or the city, state and zip...
                            if (line.IndexOf(",", StringComparison.Ordinal) < 0)
                            {
                                // This should be the country...
                                address.Country = line.Trim();

                                // Let's search this country in the list...
                                var foundCountry = false;
                                foreach (var info in countryInfo)
                                    if (string.Compare(info.Name, address.Country, true, CultureInfo.InvariantCulture) == 0)
                                    {
                                        foundCountry = true;
                                        break;
                                    }

                                if (!foundCountry)
                                {
                                    // Didn't find it? So, this is not the country... it may be the "City, ST, Zip" line.
                                    address.Country = string.Empty;

                                    // Let's try...
                                    GetZipCityState(line, address);
                                }

                                //this.country = addressObject.Country;
                            }
                            else
                                // This should be the line with "City, ST Zip"...
                                GetZipCityState(line, address);

                            break;

                        case 2:
                            if (address.Country.Length == 0)
                            {
                                // The last line was not the country, so it was the city and therefore this must be one of the 3 address' line.
                                if (counter == 3)
                                    invalidAddress = true;
                                else
                                {
                                    if (counter == 1) address.Address1 = line.Trim();
                                    else if (counter == 2) address.Address2 = line.Trim();
                                    else if (counter == 3) address.Address3 = line.Trim();
                                }
                            }
                            else
                                // this may be the "City, ST Zip" line, so let's start from right to left.
                                GetZipCityState(line, address);

                            break;

                        default:
                            if (counter > 3)
                                invalidAddress = true;
                            else
                            {
                                if (counter == 1) address.Address1 = line.Trim();
                                else if (counter == 2) address.Address2 = line.Trim();
                                else if (counter == 3) address.Address3 = line.Trim();
                            }

                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to parse a line that presumably holds a country name. If the name can be
        /// identified as a real country, the address format for that country will be returned.
        /// If the country can not be found, the address format for the specified default
        /// country will be used. 
        /// If that code can not be found either, 
        /// </summary>
        /// <param name="assumedCountryLine">Line containing what is assumed to be country information</param>
        /// <param name="threeLetterRegionCode">Three letter country code (such as "USA") which is assumed as the default if no country can be identified.</param>
        /// <param name="twoLetterIsoRegionCode">Two letter ISO country code (such as "US") which is assumed as the default if no country can be identified.</param>
        /// <returns></returns>
        protected virtual AddressFormat GetAddressFormatForCountry(string assumedCountryLine, string threeLetterRegionCode, string twoLetterIsoRegionCode)
        {
            // We first canonicalize the country line
            var countryName = assumedCountryLine.Trim().ToLower(CultureInfo.InvariantCulture);

            // We check to see whether the country can be found in the current list of countries
            foreach (CountryInformation country in countryInfo)
                if (string.Compare(country.Name, countryName, true, CultureInfo.InvariantCulture) == 0)
                    // We found a match
                    return country.Format;

            // We have not found the country so far, so we attempt to find the country by ISO code
            // We check to see whether the country can be found in the current list of countries
            foreach (CountryInformation country in countryInfo)
                if (string.Compare(country.IsoCode, twoLetterIsoRegionCode, true, CultureInfo.InvariantCulture) == 0)
                    // We found a match
                    return country.Format;

            // We could not identify a proper format, so we have to go with a default
            return AddressFormat.CityStateZip;
        }

        /// <summary>
        /// Delegate for the InvalidAddress event.
        /// </summary>
        public delegate void InvalidAddressEventHandler(object sender, InvalidAddressEventArgs e);

        /// <summary>
        /// Event that's gonna be fired whenever the Address dialog parser is required.
        /// </summary>
        public event InvalidAddressEventHandler InvalidAddress;

        /// <summary>
        /// Notifies subscribers of InvalidAddress event.
        /// </summary>
        /// <param name="e">Args containing info about the source (full address) and the Address object (parser's best guess for the parsing).</param>
        protected virtual void OnInvalidAddress(InvalidAddressEventArgs e) => InvalidAddress?.Invoke(this, e);
    }

    /// <summary>
    /// EventArgs class for the ShowAddressWindow event.
    /// </summary>
    public class InvalidAddressEventArgs : EventArgs
    {
        /// <summary>
        /// This is a string containing the full address sent to the parser.
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// This is the Address object that holds the parsed address (or whatever the parser's best guess was).
        /// </summary>
        public Address Address { get; set; }

        /// <summary>
        /// This used to be a parameter used in the legacy SFDCT's code for the parser. It might not be needed here.
        /// </summary>
        public bool Updated { get; set; }
    }

    /// <summary>
    /// Address interface
    /// </summary>
    public interface IAddress
    {
        /// <summary>
        /// Address 1
        /// </summary>
        string Address1 { get; set; }

        /// <summary>
        /// Address 2
        /// </summary>
        string Address2 { get; set; }

        /// <summary>
        /// Address 3
        /// </summary>
        string Address3 { get; set; }

        /// <summary>
        /// City
        /// </summary>
        string City { get; set; }

        /// <summary>
        /// State
        /// </summary>
        string State { get; set; }

        /// <summary>
        /// Zip
        /// </summary>
        string Zip { get; set; }

        /// <summary>
        /// Country
        /// </summary>
        string Country { get; set; }

        /// <summary>
        /// CountryID
        /// </summary>
        Guid CountryID { get; set; }

        /// <summary>
        /// Full address as a single string
        /// </summary>
        string FullAddress { get; set; }
    }

    /// <summary>
    /// Address class: used to pass an address back and forth (mainly when passing a parsed address to the Address Edit Dialog).
    /// Note that this is not a replacement for the Address entity.
    /// </summary>
    /// <remarks>This is a 'contract' or 'message' that is sent between objects. For this reason it is OK that that object has public fields.</remarks>
    public class Address
    {
        public Address()
        {
            Address1 = string.Empty;
            Address2 = string.Empty;
            Address3 = string.Empty;
            City = string.Empty;
            State = string.Empty;
            Zip = string.Empty;
            Country = string.Empty;
        }

        /// <summary>
        /// Canceled
        /// </summary>
        public bool Canceled { get; set; }

        /// <summary>
        /// Updated
        /// </summary>
        public bool Updated { get; set; }

        /// <summary>
        /// Address 1
        /// </summary>
        public string Address1 { get; set; }

        /// <summary>
        /// Address 2
        /// </summary>
        public string Address2 { get; set; }

        /// <summary>
        /// Address 3
        /// </summary>
        public string Address3 { get; set; }

        /// <summary>
        /// City
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// State
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// Zip / Postal Code
        /// </summary>
        public string Zip { get; set; }

        /// <summary>
        /// Country
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// Country ID
        /// </summary>
        public Guid CountryID { get; set; }

        //TODO: Make this thing use the GetFormattedAddress method.
        /// <summary>
        /// Returns full address.
        /// </summary>
        /// <returns>String containing concatenated full address.</returns>
        public string FullAddress
        {
            get
            {
                var address = string.Empty;

                if (!string.IsNullOrEmpty(Address1)) address += Address1.Trim();

                if (!string.IsNullOrEmpty(Address2))
                {
                    if (!string.IsNullOrEmpty(address)) address += "\r\n";
                    address += Address2.Trim();
                }

                if (!string.IsNullOrEmpty(Address3))
                {
                    if (!string.IsNullOrEmpty(address)) address += "\r\n";
                    address += Address3.Trim();
                }

                if (!string.IsNullOrEmpty(City))
                {
                    if (!string.IsNullOrEmpty(address)) address += "\r\n";
                    address += City.Trim();
                }

                if (!string.IsNullOrEmpty(State))
                {
                    if (!string.IsNullOrEmpty(address)) address += ", ";
                    address += State.Trim();
                }

                if (!string.IsNullOrEmpty(Zip))
                {
                    if (!string.IsNullOrEmpty(address)) address += " ";
                    address += Zip.Trim();
                }

                if (!string.IsNullOrEmpty(Country))
                {
                    if (!string.IsNullOrEmpty(address)) address += "\r\n";
                    address += Country.Trim();
                }

                return address;
            }
        }

        /// <summary>
        /// Check whether all fields are empty.
        /// </summary>
        /// <returns></returns>
        public bool IsClean()
        {
            if (string.IsNullOrEmpty(Address1) && string.IsNullOrEmpty(Address2) && string.IsNullOrEmpty(Address3) &&
                string.IsNullOrEmpty(City) && string.IsNullOrEmpty(State) && string.IsNullOrEmpty(Zip) &&
                (string.IsNullOrEmpty(Country) || Country == null))
                return true;
            return false;
        }

        /// <summary>
        /// Fills an Address Entity with the values from this object.
        /// </summary>
        /// <param name="entity">Address Entity that's going to be filled.</param>
        public void FillAddressEntity(INameAddressEntity entity)
        {
            if (entity == null) return;
            entity.Street = Address1;
            entity.Street2 = Address2;
            entity.Street3 = Address3;
            entity.City = City;
            entity.State = State;
            entity.Zip = Zip;

            entity.CountryID = GetCountryID(Country);
        }

        /// <summary>
        /// This method gets the PK of a given country name.
        /// Obs.: This will have to be revised once we've got multi-langual support.
        /// </summary>
        /// <param name="country">String representing the name of the country.</param>
        /// <returns>The country's PK.</returns>
        private Guid GetCountryID(string country)
        {
            // TODO: Double-check whether we want this to be static or not
            // TODO: We must use a business object here!!! 

            // TODO: Be more generic than using "database"
            var svc = DataServiceFactory.GetDataService("database");
            using (var comSelect = svc.NewCommandObject())
            {
                comSelect.CommandText = "SELECT pk_Country FROM Country where cName = @country";
                comSelect.Parameters.Add(svc.NewCommandObjectParameter("@country", country));

                using (var countries = new CountryBusinessObject())
                using (var ds = countries.ExecuteStoredProcedureQuery(comSelect))
                {
                    if (ds.Tables[0].Rows.Count > 0)
                        return (Guid) ds.Tables[0].Rows[0]["pk_Country"];
                    return Guid.Empty;
                }
            }
        }

        /// <summary>
        /// Fill this object with data from an Address entity.
        /// </summary>
        /// <param name="entity">Address Entity from where where going to take the values.</param>
        public void FillFromAddressEntity(INameAddressEntity entity)
        {
            if (entity == null) return;
            Address1 = entity.Street;
            Address2 = entity.Street2;
            Address3 = entity.Street3;
            City = entity.City;
            State = entity.State;
            Zip = entity.Zip;

            try
            {
                using (var countries = entity.NewCountryEntity())
                    Country = countries.Name;
            }
            catch
            {
                // If this is broken, it's probably because the entity has a brand new entry and therefore there's no
                // country entity available. We just default the country to an empty string.
                Country = string.Empty;
            }
        }
    }

    /// <summary>
    /// AddressParserException class. It's meant to be thrown anytime an error
    /// has occurred when parsing a name.
    /// </summary>
    [Serializable]
    public class AddressParserException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public AddressParserException() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Message</param>
        public AddressParserException(string message) : base(message) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="inner">Inner Exception</param>
        public AddressParserException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Streaming context</param>
        protected AddressParserException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// This class contains basic country information such as the name, code, and address type
    /// </summary>
    public class CountryInformation
    {
        /// <summary>
        /// ISO code
        /// </summary>
        private string isoCode = string.Empty;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Country name</param>
        /// <param name="isoCode">ISO Code</param>
        /// <param name="format">Address Format</param>
        public CountryInformation(string name, string isoCode, AddressFormat format)
        {
            Name = name;
            IsoCode = isoCode;
            Format = format;
        }

        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// ISO Code
        /// </summary>
        public string IsoCode
        {
            get => isoCode;
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                if (value.Length > 2) value = value.Substring(0, 2);
                isoCode = value;
            }
        }

        /// <summary>
        /// Address Format used by this country
        /// </summary>
        public AddressFormat Format { get; set; }
    }

    [Obsolete("Use the default AddressParser class instead.")]
    public class USAddressParser : AddressParser { }
}