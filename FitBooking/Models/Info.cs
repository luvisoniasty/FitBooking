//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using FitBooking.Models;

public partial class Info
{
    public int Id { get; set; }
    public string oMnie { get; set; }
    public Nullable<int> id_uzytkownik { get; set; }

    public virtual Uzytkownik Uzytkownik { get; set; }
}
