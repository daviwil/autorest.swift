//
//  ExternalDocumentation.swift
//
//
//  Created by Travis Prescott on 7/10/20.
//

import Foundation

/// A reference to external documentation
public struct ExternalDocumentation: Codable {
    public let description: String?

    /// A URI
    public let url: String

    /// Additional metadata extensions dictionary
    public let extensions: [String: Bool]?
}