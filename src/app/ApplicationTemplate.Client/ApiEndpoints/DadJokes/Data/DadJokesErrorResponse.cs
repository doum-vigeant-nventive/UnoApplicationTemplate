﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ApplicationTemplate.Client;

public partial class DadJokesErrorResponse
{
	public DadJokesErrorResponse(string message)
	{
		Message = message;
	}

	public string Message { get; }
}
