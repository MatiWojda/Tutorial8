﻿using System.ComponentModel.DataAnnotations;

namespace Tutorial8.Models.DTOs;

public class ClientDTO
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Telephone { get; set; }
    public string Pesel { get; set; }
}

public class TripDTO
{
    public int IdTrip { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int MaxPeople { get; set; }
    public List<CountryDTO> Countries { get; set; }
    
}
public class ClientTripDTO : TripDTO
{
    public int? PaymentDate { get; set; } 
    public int? RegisteredAt { get; set; } 
}

public class CountryDTO
{
    public int IdCountry { get; set; }
    public string Name { get; set; }
}